using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BoboBrowse.Api;
using BoboBrowse.Facets;
using BoboBrowse.Facets.filter;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.query.scoring
{
    public class FacetTermQuery : Query
    {
        private readonly string name;
        private readonly BrowseSelection sel;
        private readonly IFacetTermScoringFunctionFactory scoringFactory;
        private readonly Dictionary<string, float> boostMap;

        public FacetTermQuery(BrowseSelection sel, Dictionary<string, float> boostMap)
            : this(sel, boostMap, new DefaultFacetTermScoringFunctionFactory())
        {
        }

        public FacetTermQuery(BrowseSelection sel, Dictionary<string, float> boostMap, IFacetTermScoringFunctionFactory scoringFactory)
        {
            name = sel.FieldName;
            this.sel = sel;
            this.scoringFactory = scoringFactory;
            this.boostMap = boostMap;
        }

        public override string ToString(string fieldname)
        {
            return Convert.ToString(sel);
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new FacetTermWeight(this, searcher.GetSimilarity());
        }

        public override void ExtractTerms(Hashtable terms)
        {
            foreach (string val in sel.Values)
            {
                terms.Add(name, val);
            }
        }

        private class FacetTermWeight : Weight
        {
            internal Similarity _similarity;
            private FacetTermQuery parent;

            public FacetTermWeight(FacetTermQuery parent, Similarity sim)
            {
                this.parent = parent;
                _similarity = sim;
            }

            public override Explanation Explain(IndexReader reader, int docid)
            {
                BoboIndexReader boboReader = (BoboIndexReader)reader;
                FacetHandler fhandler = boboReader.GetFacetHandler(parent.name);
                if (fhandler != null)
                {
                    BoboDocScorer scorer = null;
                    if (fhandler is IFacetScoreable)
                    {
                        scorer = ((IFacetScoreable)fhandler).GetDocScorer(parent.scoringFactory, parent.boostMap);
                        return scorer.Explain(docid);
                    }
                    else
                    {
                        return null;
                    }
                }
                return null;
            }

            public override Query GetQuery()
            {
                return parent;
            }

            public override float GetValue()
            {
                return 0;
            }

            public override void Normalize(float score)
            {
                // TODO Auto-generated method stub

            }

            public override Scorer Scorer(IndexReader reader, bool b1, bool b2)
            {
                if (reader is BoboIndexReader)
                {
                    BoboIndexReader boboReader = (BoboIndexReader)reader;
                    FacetHandler fhandler = boboReader.GetFacetHandler(parent.name);
                    if (fhandler != null)
                    {
                        DocIdSetIterator dociter = null;
                        RandomAccessFilter filter = fhandler.BuildFilter(parent.sel);
                        if (filter != null)
                        {
                            DocIdSet docset = filter.GetDocIdSet(boboReader);
                            if (docset != null)
                            {
                                dociter = docset.Iterator();
                            }
                        }
                        if (dociter == null)
                        {
                            dociter = new FastMatchAllDocsQuery.FastMatchAllScorer(boboReader.MaxDoc(), new int[0], 1.0f);
                        }
                        BoboDocScorer scorer = null;
                        if (fhandler is IFacetScoreable)
                        {
                            scorer = ((IFacetScoreable)fhandler).GetDocScorer(parent.scoringFactory, parent.boostMap);
                        }
                        return new FacetTermScorer(_similarity, dociter, scorer);
                    }
                    return null;
                }
                else
                {
                    throw new IOException("index reader not instance of " + typeof(BoboIndexReader));
                }
            }

            public override float SumOfSquaredWeights()
            {
                // TODO Auto-generated method stub
                return 0;
            }
        }

        private class FacetTermScorer : Scorer
        {
            private readonly DocIdSetIterator docSetIter;
            private readonly BoboDocScorer scorer;

            protected internal FacetTermScorer(Similarity similarity, DocIdSetIterator docidsetIter, BoboDocScorer scorer)
                : base(similarity)
            {
                docSetIter = docidsetIter;
                this.scorer = scorer;
            }

            public override Explanation Explain(int docid)
            {
                Explanation expl = null;
                if (scorer != null)
                {
                    expl = scorer.Explain(docid);
                }
                return expl;
            }

            public override float Score()
            {
                return scorer == null ? 1.0f : scorer.Score(Doc());
            }

            public override int Doc()
            {
                return docSetIter.Doc();
            }

            public override bool Next()
            {
                return docSetIter.Next();
            }

            public override bool SkipTo(int target)
            {
                return docSetIter.SkipTo(target);
            }
        }
    }
}