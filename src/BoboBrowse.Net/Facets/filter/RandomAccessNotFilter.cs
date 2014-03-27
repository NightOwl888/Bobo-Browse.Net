using BoboBrowse.docidset;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneExt.Impl;

namespace BoboBrowse.Facets.filter
{
    public class RandomAccessNotFilter 
        : RandomAccessFilter
    {
        protected internal readonly RandomAccessFilter _innerFilter;

        public RandomAccessNotFilter(RandomAccessFilter innerFilter)
        {
            _innerFilter = innerFilter;
        }

        private class NotRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly RandomAccessDocIdSet innerDocIdSet;
            private readonly DocIdSet notInnerDocIdSet;

            public NotRandomAccessDocIdSet(RandomAccessDocIdSet innerDocIdSet, DocIdSet notInnerDocIdSet)
            {
                this.innerDocIdSet = innerDocIdSet;
                this.notInnerDocIdSet = notInnerDocIdSet;
            }

            public override bool Get(int docId)
            {
                return !innerDocIdSet.Get(docId);
            }
            public override DocIdSetIterator Iterator()
            {
                return notInnerDocIdSet.Iterator();
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            RandomAccessDocIdSet innerDocIdSet = _innerFilter.GetRandomAccessDocIdSet(reader);
            DocIdSet notInnerDocIdSet = new NotDocIdSet(innerDocIdSet, reader.MaxDoc());
            return new NotRandomAccessDocIdSet(innerDocIdSet, notInnerDocIdSet);
        }
    }
}