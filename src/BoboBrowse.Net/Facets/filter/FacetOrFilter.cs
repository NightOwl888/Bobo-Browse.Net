using BoboBrowse.docidset;
using BoboBrowse.Facets.data;
using BoboBrowse.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace BoboBrowse.Facets.filter
{
    public class FacetOrFilter : RandomAccessFilter
    {
        protected internal readonly FacetDataCache dataCache;
        protected internal readonly BigSegmentedArray orderArray;
        protected internal readonly int[] index;
        private OpenBitSet bitset;

        public FacetOrFilter(FacetDataCache dataCache, int[] index)
            : this(dataCache, index, false)
        {
        }

        public FacetOrFilter(FacetDataCache dataCache, int[] index, bool takeCompliment)
        {
            this.dataCache = dataCache;
            orderArray = dataCache.orderArray;
            this.index = index;
            bitset = new OpenBitSet(this.dataCache.valArray.Count);
            foreach (int i in this.index)
            {
                bitset.FastSet(i);
            }
            if (takeCompliment)
            {
                bitset.Flip(0, this.dataCache.valArray.Count);
            }
        }

        private class EmptyDocIdSetContainer : RandomAccessDocIdSet
        {
            private readonly DocIdSet empty = EmptyDocIdSet.GetInstance();

            public override bool Get(int docId)
            {
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return empty.Iterator();
            }
        }

        private class FacetDocIdSetContainer : RandomAccessDocIdSet
        {
            private FacetOrFilter parent;

            public FacetDocIdSetContainer(FacetOrFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetOrDocIdSetIterator(parent.dataCache, parent.index, parent.bitset);
            }

            public override bool Get(int docId)
            {
                return parent.bitset.FastGet(parent.orderArray.Get(docId));
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (index.Length == 0)
            {
                return new EmptyDocIdSetContainer();
            }
            else
            {
                return new FacetDocIdSetContainer(this);
            }
        }

        public class FacetOrDocIdSetIterator : DocIdSetIterator
        {
            protected internal int _doc;
            protected internal readonly FacetDataCache _dataCache;
            protected internal readonly int[] _index;
            protected internal int _maxID;
            protected internal readonly OpenBitSet _bitset;
            protected internal readonly BigSegmentedArray _orderArray;

            public FacetOrDocIdSetIterator(FacetDataCache dataCache, int[] index, OpenBitSet bitset)
            {
                _dataCache = dataCache;
                _index = index;
                _orderArray = dataCache.orderArray;
                _bitset = bitset;

                _doc = int.MaxValue;
                _maxID = -1;
                foreach (int i in _index)
                {
                    if (_doc > _dataCache.minIDs[i])
                    {
                        _doc = _dataCache.minIDs[i];
                    }
                    if (_maxID < _dataCache.maxIDs[i])
                    {
                        _maxID = _dataCache.maxIDs[i];
                    }
                }
                _doc--;
                if (_doc < 0)
                    _doc = -1;
            }

            public override int Doc()
            {
                return _doc;
            }
            //      
            //      protected boolean validate(int docid){
            //          return _dataCache.orderArray.get(docid) == _index;
            //      }
            //
            public override bool Next()
            {
                _doc = _orderArray.FindValues(_bitset, _doc + 1, _maxID);
                return (_doc <= _maxID);
            }

            public override bool SkipTo(int id)
            {
                if (id < _doc)
                    id = _doc + 1;
                _doc = _orderArray.FindValues(_bitset, id, _maxID);
                return (_doc <= _maxID);
            }
        }
    }
}