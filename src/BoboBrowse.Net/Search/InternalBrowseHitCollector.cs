using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets;
using BoboBrowse.impl;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Search
{
    public class InternalBrowseHitCollector : TopDocsSortedHitCollector
    {
        private readonly SortedHitQueue hitQueue;
        private readonly BrowseHit[] sortedDocs;
        private readonly SortField[] sortFields;
        private int totalHits;
        private FieldDoc reusableFieldDoc;
        private readonly BoboIndexReader reader;
        private readonly int offset;
        private readonly int count;
        private readonly BoboBrowser boboBrowser;
        private readonly bool fetchStoredFields;

        private Scorer scorer;

        public InternalBrowseHitCollector(BoboBrowser boboBrowser, SortField[] sort, int offset, int count, bool fetchStoredFields)
        {
            this.boboBrowser = boboBrowser;
            reader = boboBrowser.GetIndexReader();
            sortFields = QueryProducer.convertSort(sort, reader);
            this.offset = offset;
            this.count = count;
            hitQueue = new SortedHitQueue(this.boboBrowser, sortFields, offset + count);
            totalHits = 0;
            this.fetchStoredFields = fetchStoredFields;
        }

        public override void SetScorer(Scorer _scorer) 
        {
            this.scorer = _scorer;
        }

        public override void Collect(int doc)
        {
            totalHits++;
            var score = scorer.Score();
            if (reusableFieldDoc == null)
            {
                reusableFieldDoc = new FieldDoc(doc, score);
            }
            else
            {
                reusableFieldDoc.score = score;
                reusableFieldDoc.doc = doc;
            }
            reusableFieldDoc = (FieldDoc)hitQueue.InsertWithOverflow(reusableFieldDoc);
        }

        public override void SetNextReader(IndexReader _reader, int docBase)
        {
            // todo: is needed?
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return true;
        }

        public override int GetTotalHits()
        {
            return totalHits;
        }

        private void FillInRuntimeFacetValues(BrowseHit[] hits)
        {
            ICollection<FacetHandler> runtimeFacetHandlers = boboBrowser.GetRuntimeFacetHandlerMap().Values;
            foreach (BrowseHit hit in hits)
            {
                Dictionary<string, string[]> map = hit.FieldValues;
                int docid = hit.DocId;
                foreach (FacetHandler facetHandler in runtimeFacetHandlers)
                {
                    string[] values = facetHandler.GetFieldValues(docid);
                    if (values != null)
                    {
                        map.Add(facetHandler.Name, values);
                    }
                }
            }
        }

        public override BrowseHit[] GetTopDocs()
        {
            FieldDoc[] fdocs = hitQueue.GetTopDocs(offset, count);
            return BuildHits(fdocs);
        }

        public virtual BrowseHit[] BuildHits(FieldDoc[] fdocs)
        {
            BrowseHit[] hits = new BrowseHit[fdocs.Length];
            int i = 0;

            ICollection<FacetHandler> facetHandlers = reader.GetFacetHandlerMap().Values;
            foreach (FieldDoc fdoc in fdocs)
            {
                BrowseHit hit = new BrowseHit();
                if (fetchStoredFields)
                {
                    hit.StoredFields = reader.Document(fdoc.doc);
                }
                Dictionary<string, string[]> map = new Dictionary<string, string[]>();
                foreach (FacetHandler facetHandler in facetHandlers)
                {
                    map.Add(facetHandler.Name, facetHandler.GetFieldValues(fdoc.doc));
                }
                hit.FieldValues = map;
                hit.DocId = fdoc.doc;
                hit.Score = fdoc.score;
                foreach (SortField f in sortFields)
                {
                    if (f.GetType() != SortField.SCORE && f.GetType() != SortField.DOC)
                    {
                        string fieldName = f.GetField();
                        ScoreDocComparator comparator;
                        hitQueue.comparatorMap.TryGetValue(fieldName, out comparator);
                        if (comparator != null)
                        {
                            hit.AddComparable(fieldName, comparator.SortValue(fdoc));
                        }
                    }
                }
                hits[i++] = hit;
            }
            FillInRuntimeFacetValues(hits);
            return hits;
        }
    }
}