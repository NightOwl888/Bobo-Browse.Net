#region using

using System.Collections.Generic;
using System.IO;
using BoboBrowse.Api;
using BoboBrowse.impl;
using BoboBrowse.Util;
using Lucene.Net.Index;
using log4net;
using Lucene.Net.Search;

#endregion

namespace BoboBrowse.Search
{
    public class MultiTopDocsSortedHitCollector : TopDocsSortedHitCollector
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof (MultiTopDocsSortedHitCollector));
        private int totalCount;
        private readonly MultiBoboBrowser multiBrowser;
        private readonly TopDocsSortedHitCollector[] subCollectors;
        private readonly int[] starts;
        private readonly int offset;
        private readonly int count;
        private readonly SortField[] sort;

        public MultiTopDocsSortedHitCollector(MultiBoboBrowser multiBrowser, SortField[] sort, int offset, int count,
                                              bool fetchStoredFields)
        {
            this.sort = sort;
            this.offset = offset;
            this.count = count;
            this.multiBrowser = multiBrowser;
            IBrowsable[] subBrowsers = this.multiBrowser.getSubBrowsers();
            subCollectors = new TopDocsSortedHitCollector[subBrowsers.Length];
            for (int i = 0; i < subBrowsers.Length; ++i)
            {
                subCollectors[i] = subBrowsers[i].GetSortedHitCollector(sort, 0, this.offset + this.count, fetchStoredFields);
            }
            starts = this.multiBrowser.getStarts();
            totalCount = 0;
        }

        public override void SetScorer(Scorer scorer)
        {
            //throw new System.NotImplementedException();
        }

        public override void Collect(int doc)
        {
            int mapped = multiBrowser.SubDoc(doc);
            int index = multiBrowser.SubSearcher(doc);
            subCollectors[index].Collect(mapped);
            totalCount++;
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            //throw new System.NotImplementedException();
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return true;
        }

        public override BrowseHit[] GetTopDocs()
        {
            List<IEnumerable<BrowseHit>> iteratorList = new List<IEnumerable<BrowseHit>>(subCollectors.Length);

            for (int i = 0; i < subCollectors.Length; ++i)
            {
                int @base = starts[i];
                try
                {
                    BrowseHit[] subHits = subCollectors[i].GetTopDocs();
                    foreach (BrowseHit hit in subHits)
                    {
                        hit.DocId = hit.DocId + @base;
                    }
                    iteratorList.Add(subHits);
                }
                catch (IOException ioe)
                {
                    logger.Error(ioe.Message, ioe);
                }
            }

            SortField[] sf = sort;
            if (sf == null || sf.Length == 0)
            {
                sf = new SortField[] {SortField.FIELD_SCORE};
            }
            IComparer<BrowseHit> comparator = new SortedFieldBrowseHitComparator(sf);

            List<BrowseHit> mergedList = ListMerger.MergeLists(offset, count, iteratorList.ToArray(), comparator);
            return mergedList.ToArray();
        }

        public override int GetTotalHits()
        {
            return totalCount;
        }
    }
}