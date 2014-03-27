using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BoboBrowse.docidset;
using BoboBrowse.LangUtils;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace BoboBrowse.query
{
    ///<summary>A query that matches all documents.</summary>
    public sealed class FastMatchAllDocsQuery : Query
    {
        private readonly int[] deletedDocs;

        public FastMatchAllDocsQuery(int[] deletedDocs, int maxDoc)
        {
            this.deletedDocs = deletedDocs;
        }

        public sealed class FastMatchAllScorer : Scorer
        {
            private int deletedIndex;
            private bool moreDeletions;
            internal int doc;
            internal readonly float score;
            internal readonly int[] deletedDocs;
            private readonly int maxDoc;
            private readonly int delLen;

            public FastMatchAllScorer(int maxdoc, int[] delDocs, float score)
                : this(maxdoc, delDocs, new DefaultSimilarity(), score)
            {
            }

            public FastMatchAllScorer(int maxdoc, int[] delDocs, Similarity similarity, float score)
                : base(similarity)
            {
                doc = -1;
                deletedDocs = delDocs;
                deletedIndex = 0;
                moreDeletions = deletedDocs != null && deletedDocs.Length > 0;
                delLen = deletedDocs != null ? deletedDocs.Length : 0;
                this.score = score;
                maxDoc = maxdoc;
            }

            public override Explanation Explain(int doc)
            {
                return null; // not called... see MatchAllDocsWeight.explain()
            }

            public override int Doc()
            {
                return doc;
            }

            public override bool Next()
            {
                while (++doc < maxDoc)
                {
                    if (!moreDeletions || doc < deletedDocs[deletedIndex])
                    {
                        return true;
                    }
                    else // _moreDeletions == true && _doc >= _deletedDocs[_deletedIndex]
                    {
                        while (moreDeletions && doc > deletedDocs[deletedIndex]) // catch up _deletedIndex to _doc
                        {
                            deletedIndex++;
                            moreDeletions = deletedIndex < delLen;
                        }
                        if (!moreDeletions || doc < deletedDocs[deletedIndex])
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public override float Score()
            {
                return score;
            }

            public override bool SkipTo(int target)
            {
                if (target > doc)
                {
                    doc = target - 1;
                    return Next();
                }

                return (target == doc) ? Next() : false;
            }

        }

        private class FastMatchAllDocsWeight : Weight
        {
            private readonly FastMatchAllDocsQuery parent;
            private readonly Similarity similarity;
            private float queryWeight;
            private float queryNorm;

            public FastMatchAllDocsWeight(FastMatchAllDocsQuery parent, Searcher searcher)
            {
                this.parent = parent;
                similarity = searcher.GetSimilarity();
            }

            public override string ToString()
            {
                return "weight(" + parent + ")";
            }

            public override Query GetQuery()
            {
                return parent;
            }

            public override float GetValue()
            {
                return queryWeight;
            }

            public override float SumOfSquaredWeights()
            {
                queryWeight = parent.GetBoost();
                return queryWeight * queryWeight;
            }

            public override void Normalize(float queryNorm)
            {
                this.queryNorm = queryNorm;
                queryWeight *= this.queryNorm;
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                return new FastMatchAllScorer(reader.MaxDoc(), parent.deletedDocs, similarity, GetValue());
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                // explain query weight
                Explanation queryExpl = new Explanation(GetValue(), "FastMatchAllDocsQuery");
                if (parent.GetBoost() != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(parent.GetBoost(), "boost"));
                }
                queryExpl.AddDetail(new Explanation(queryNorm, "queryNorm"));

                return queryExpl;
            }
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new FastMatchAllDocsWeight(this, searcher);
        }

        public override void ExtractTerms(Hashtable terms)
        {
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("MatchAllDocsQuery");
            buffer.Append(ToStringUtils.Boost(GetBoost()));
            return buffer.ToString();
        }

        public override bool Equals(object o)
        {
            if (!(o is FastMatchAllDocsQuery))
            {
                return false;
            }
            FastMatchAllDocsQuery other = (FastMatchAllDocsQuery)o;
            return this.GetBoost() == other.GetBoost();
        }

        public override int GetHashCode()
        {
            return FloatUtils.floatToIntBits(GetBoost()) ^ 0x1AA71190;
        }

        private class TestDocIdSetIterator : FilteredDocSetIterator
        {
            private readonly List<int> dupDocs;
            private readonly int min;
            private readonly int max;

            public TestDocIdSetIterator(List<int> dupDocs, DocIdSetIterator innerIter)
                : base(innerIter)
            {
                this.dupDocs = dupDocs;
                if (this.dupDocs != null && this.dupDocs.Count > 0)
                {
                    int[] arr = this.dupDocs.ToArray();
                    min = arr[0];
                    max = arr[arr.Length - 1];
                }
                else
                {
                    min = int.MaxValue;
                    max = -1;
                }
            }

            protected internal override sealed bool Match(int docid)
            {
                return !(dupDocs != null && docid >= min && docid <= max && dupDocs.Contains(docid));
                //	 return !(_dupDocs != null && _dupDocs.contains(docid));
                //	return true;
            }
        }

        // FIXME : some tests
        static void Main4(string[] args)
        {
            int maxDoc = 5000000;
            C5.HashSet<int> delSet = new C5.HashSet<int>();
            int numDel = 1000;
            Random rand = new Random();
            for (int i = 0; i < numDel; ++i)
            {
                delSet.Add(i * 30);
            }
            long numIter = 1000000;
            int[] delArray = delSet.ToArray();
            for (long i = 0; i < numIter; ++i)
            {
                long start = System.Environment.TickCount;
                FastMatchAllScorer innerIter = new FastMatchAllScorer(maxDoc, delArray, 1.0f);
                //  TestDocIdSetIterator testIter = new TestDocIdSetIterator(delSet,innerIter);
                //	  while(testIter.next())
                //	  {
                //		  testIter.Doc();
                //	  }
                //
                for (int k = 0; k < maxDoc; ++k)
                {
                    innerIter.SkipTo(k * 3);
                }
                long end = System.Environment.TickCount;
                Console.WriteLine("took: " + (end - start));
            }
        }
    }
}