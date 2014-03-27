using System;
using BoboBrowse.docidset;
using BoboBrowse.Facets.data;
using BoboBrowse.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.filter
{
    public sealed class FacetRangeFilter : RandomAccessFilter
    {
        private readonly FacetDataCache _dataCache;
        private readonly int _start;
        private readonly int _end;

        public FacetRangeFilter(FacetDataCache dataCache, int start, int end)
        {
            _dataCache = dataCache;
            _start = start;
            _end = end;
        }

        private sealed class FacetRangeDocIdSetIterator : DocIdSetIterator
        {
            private int _doc = -1;
            private int _totalFreq;
            private int _minID = int.MaxValue; // FIXME : ??? max value 
            private int _maxID = -1;
            private readonly int _start;
            private readonly int _end;
            private readonly BigSegmentedArray _orderArray;

            internal FacetRangeDocIdSetIterator(int start, int end, FacetDataCache dataCache)
            {
                _totalFreq = 0;
                _start = start;
                _end = end;
                for (int i = start; i <= end; ++i)
                {
                    _totalFreq += dataCache.freqs[i];
                    _minID = Math.Min(_minID, dataCache.minIDs[i]);
                    _maxID = Math.Max(_maxID, dataCache.maxIDs[i]);
                }
                _doc = Math.Max(-1, _minID - 1);
                _orderArray = dataCache.orderArray;
            }

            public override int Doc()
            {
                return _doc;
            }

            public override bool Next()
            {
                _doc = _orderArray.FindValueRange(_start, _end, _doc + 1, _maxID);
                return (_doc <= _maxID);
            }

            public override bool SkipTo(int id)
            {
                if (id < _doc)
                    id = _doc + 1;
                _doc = _orderArray.FindValueRange(_start, _end, id, _maxID);
                return (_doc <= _maxID);
            }
        }

        private class RangeRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetRangeFilter parent;

            public RangeRandomAccessDocIdSet(FacetRangeFilter parent)
            {
                this.parent = parent;
            }

            public override bool Get(int docId)
            {
                int index = parent._dataCache.orderArray.Get(docId);
                return index >= parent._start && index <= parent._end;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetRangeDocIdSetIterator(parent._start, parent._end, parent._dataCache);
            }

        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            return new RangeRandomAccessDocIdSet(this);
        }
    }
}