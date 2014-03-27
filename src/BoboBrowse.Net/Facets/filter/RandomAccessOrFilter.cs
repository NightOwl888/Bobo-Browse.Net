using System;
using System.Collections.Generic;
using BoboBrowse.docidset;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneExt.Impl;

namespace BoboBrowse.Facets.filter
{
    public class RandomAccessOrFilter : RandomAccessFilter
    {
        protected internal readonly List<RandomAccessFilter> _filters;

        public RandomAccessOrFilter(List<RandomAccessFilter> filters)
        {
            if (filters == null)
            {
                throw new ArgumentNullException("filters");
            }
            _filters = filters;
        }

        private class RandomOrFilterDocIdSet : RandomAccessDocIdSet
        {
            private RandomAccessDocIdSet[] randomAccessDocIdSets;
            private DocIdSet orDocIdSet;

            public RandomOrFilterDocIdSet(RandomAccessDocIdSet[] randomAccessDocIdSets, DocIdSet orDocIdSet)
            {
                this.orDocIdSet = orDocIdSet;
                this.randomAccessDocIdSets = randomAccessDocIdSets;
            }
            public override bool Get(int docId)
            {
                foreach (RandomAccessDocIdSet s in randomAccessDocIdSets)
                {
                    if (s.Get(docId))
                        return true;
                }
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return orDocIdSet.Iterator();
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
                DocIdSet orDocIdSet = new OrDocIdSet(list);
                return new RandomOrFilterDocIdSet(randomAccessDocIdSets, orDocIdSet);
            }
        }
    }
}