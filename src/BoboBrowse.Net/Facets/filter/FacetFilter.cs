using System;
using BoboBrowse.docidset;
using BoboBrowse.Facets.data;
using BoboBrowse.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.filter
{
    public class FacetFilter : RandomAccessFilter
	{
		protected internal readonly FacetDataCache dataCache;
		protected internal readonly BigSegmentedArray orderArray;
		protected internal readonly int index;

		public FacetFilter(FacetDataCache dataCache, int index)
		{
			this.dataCache = dataCache;
			orderArray = dataCache.orderArray;
			this.index=index;
		}

        public class FacetDocIdSetIterator : DocIdSetIterator
        {
            protected internal int doc;
            protected internal readonly int index;
            protected internal readonly int maxID;
            protected internal readonly BigSegmentedArray orderArray;

            public FacetDocIdSetIterator(FacetDataCache dataCache, int index)
            {
                this.index = index;
                doc = Math.Max(-1, dataCache.minIDs[this.index] - 1);
                maxID = dataCache.maxIDs[this.index];
                orderArray = dataCache.orderArray;
            }

            public override int Doc()
            {
                return doc;
            }
            //        
            //		protected boolean validate(int docid){
            //			return _dataCache.orderArray.get(docid) == _index;
            //		}
            //
            public override bool Next()
            {
                doc = orderArray.FindValue(index, doc + 1, maxID);
                return (doc <= maxID);
            }

            public override bool SkipTo(int id)
            {
                if (id < doc)
                {
                    id = doc + 1;
                }
                doc = orderArray.FindValue(index, id, maxID);
                return (doc <= maxID);
            }
        }

        private class EmptyFacetFilterDocIdSet : RandomAccessDocIdSet
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

        private class CacheFacetFilterDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetFilter parent;

            public CacheFacetFilterDocIdSet(FacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetDocIdSetIterator(parent.dataCache, parent.index);
            }

            public override bool Get(int docId)
            {
                return parent.orderArray.Get(docId) == parent.index;
            }
        }

		public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
		{
			if (index < 0)
			{
			    return new EmptyFacetFilterDocIdSet();
			}
			else
			{
			    return new CacheFacetFilterDocIdSet(this);
			}
		}
	}
}