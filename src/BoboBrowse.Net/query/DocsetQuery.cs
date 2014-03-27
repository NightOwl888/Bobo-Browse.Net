using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace BoboBrowse.query
{
    public class DocsetQuery : Query
    {
        private readonly DocIdSetIterator _iter;

        public DocsetQuery(DocIdSet docSet)
            : this(docSet.Iterator())
        {
        }

        public DocsetQuery(DocIdSetIterator iter)
        {
            _iter = iter;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("docset query:");
            buffer.Append(ToStringUtils.Boost(GetBoost()));
            return buffer.ToString();
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new DocSetIteratorWeight(this, searcher.GetSimilarity(), _iter);
        }

        private class DocSetIteratorWeight : Weight
        {
            private readonly Query _query;
            private readonly DocIdSetIterator _iter;
            private readonly Similarity _similarity;

            private float _queryWeight;
            private float _queryNorm;

            internal DocSetIteratorWeight(Query query, Similarity similarity, DocIdSetIterator iter)
            {
                _query = query;
                _similarity = similarity;
                _iter = iter;
                _queryNorm = 1.0f;
                _queryWeight = _query.GetBoost();
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                // explain query weight
                Explanation queryExpl = new ComplexExplanation(true, GetValue(), "docset query, product of:");
                float boost = _query.GetBoost();
                if (boost != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(boost, "boost"));
                }
                queryExpl.AddDetail(new Explanation(_queryNorm, "queryNorm"));

                return queryExpl;
            }

            public override Query GetQuery()
            {
                return _query;
            }

            public override float GetValue()
            {
                return _queryWeight;
            }

            public override void Normalize(float norm)
            {
                // we just take the boost, not going to normalize the score

                //_queryNorm = norm;
                //_queryWeight *= _queryNorm;
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                return new DocSetIteratorScorer(_similarity, _iter, this, reader);
            }

            public override float SumOfSquaredWeights()
            {
                return _queryWeight * _queryWeight;
            }

            private class DocSetIteratorScorer : Scorer
            {
                private readonly DocIdSetIterator _iter;
                private readonly float _score;
                private readonly IndexReader _reader;

                internal DocSetIteratorScorer(Similarity similarity, DocIdSetIterator iter, Weight weight, IndexReader reader)
                    : base(similarity)
                {
                    _iter = iter;
                    _score = weight.GetValue();
                    _reader = reader;
                }

                public override int Doc()
                {
                    return _iter.Doc();
                }

                public override Explanation Explain(int doc)
                {
                    return null;
                }

                public override bool Next()
                {
                    while (true)
                    {
                        bool hasNext = _iter.Next();
                        if (!hasNext)
                        {
                            return false;
                        }
                        else
                        {
                            if (!_reader.IsDeleted(_iter.Doc()))
                            {
                                return true;
                            }
                        }
                    }
                }

                public override float Score()
                {
                    return _score;
                }

                public override bool SkipTo(int target)
                {
                    bool flag = _iter.SkipTo(target);
                    if (flag)
                    {
                        if (_reader.IsDeleted(_iter.Doc()))
                        {
                            return Next();
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}