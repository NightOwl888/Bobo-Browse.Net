using BoboBrowse.docidset;
using BoboBrowse.Facets.data;
using BoboBrowse.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace BoboBrowse.Facets.filter
{
    public class MultiValueORFacetFilter : RandomAccessFilter
    {
        private readonly MultiValueFacetDataCache _dataCache;
        private readonly BigNestedIntArray _nestedArray;
        private readonly OpenBitSet _bitset;
        private readonly int[] _index;

        public MultiValueORFacetFilter(MultiValueFacetDataCache dataCache, int[] index)
        {
            _dataCache = dataCache;
            _nestedArray = dataCache._nestedArray;
            _index = index;
            _bitset = new OpenBitSet(_dataCache.valArray.Count);
            foreach (int i in _index)
            {
                _bitset.FastSet(i);
            }
        }

        private sealed class MultiValueFacetDocIdSetIterator : FacetOrFilter.FacetOrDocIdSetIterator
        {
            private readonly BigNestedIntArray _nestedArray;
            public MultiValueFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, int[] index, OpenBitSet bs)
                : base(dataCache, index, bs)
            {
                _nestedArray = dataCache._nestedArray;
            }

            public override bool Next()
            {
                while (_doc < _maxID) // not yet reached end
                {
                    if (_nestedArray.Contains(++_doc, _bitset))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override bool SkipTo(int id)
            {
                if (_doc < id)
                {
                    _doc = id - 1;
                }

                while (_doc < _maxID) // not yet reached end
                {
                    if (_nestedArray.Contains(++_doc, _bitset))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private class EmptyRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private DocIdSet empty = EmptyDocIdSet.GetInstance();

            public override bool Get(int docId)
            {
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return empty.Iterator();
            }
        }

        private class MultiRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private MultiValueORFacetFilter parent;

            public MultiRandomAccessDocIdSet(MultiValueORFacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new MultiValueFacetDocIdSetIterator(parent._dataCache, parent._index, parent._bitset);
            }

            public override bool Get(int docId)
            {
                return parent._nestedArray.Contains(docId, parent._bitset);
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (_index.Length == 0)
            {
                return new EmptyRandomAccessDocIdSet();
            }
            else
            {
                return new MultiRandomAccessDocIdSet(this);
            }
        }
    }
}