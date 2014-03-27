using System.Collections.Generic;
using BoboBrowse.docidset;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneExt.Impl;

namespace BoboBrowse.Facets.filter
{
    public class RandomAccessAndFilter : RandomAccessFilter
    {
        protected internal List<RandomAccessFilter> _filters;

        public RandomAccessAndFilter(List<RandomAccessFilter> filters)
        {
            _filters = filters;
        }

        private class RandomAccessAndFilterSet : RandomAccessDocIdSet
        {
            private RandomAccessDocIdSet[] randomAccessDocIdSets;
            private DocIdSet andDocIdSet;

            public RandomAccessAndFilterSet(RandomAccessDocIdSet[] randomAccessDocIdSets, DocIdSet andDocIdSet)
            {
                this.randomAccessDocIdSets = randomAccessDocIdSets;
                this.andDocIdSet = andDocIdSet;
            }

            public override bool Get(int docId)
            {
                foreach (RandomAccessDocIdSet s in randomAccessDocIdSets)
                {
                    if (!s.Get(docId))
                        return false;
                }
                return true;
            }

            public override DocIdSetIterator Iterator()
            {
                return andDocIdSet.Iterator();
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (_filters.Count == 1)
            {
                return _filters[0].GetRandomAccessDocIdSet(reader);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(_filters.Count);
                List<RandomAccessDocIdSet> randomAccessList = new List<RandomAccessDocIdSet>(_filters.Count);
                foreach (RandomAccessFilter f in _filters)
                {
                    RandomAccessDocIdSet s = f.GetRandomAccessDocIdSet(reader);
                    list.Add(s);
                    randomAccessList.Add(s);
                }
                RandomAccessDocIdSet[] randomAccessDocIdSets = randomAccessList.ToArray();
                DocIdSet andDocIdSet = new AndDocIdSet(list);
                return new RandomAccessAndFilterSet(randomAccessDocIdSets, andDocIdSet);
            }
        }
    }
}