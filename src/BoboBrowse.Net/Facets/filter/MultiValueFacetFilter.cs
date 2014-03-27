using BoboBrowse.docidset;
using BoboBrowse.Facets.data;
using BoboBrowse.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.filter
{
    public class MultiValueFacetFilter : RandomAccessFilter
    {
        private readonly MultiValueFacetDataCache _dataCache;
        private readonly BigNestedIntArray _nestedArray;
        private readonly int _index;

        public MultiValueFacetFilter(MultiValueFacetDataCache dataCache, int index)
        {
            _dataCache = dataCache;
            _nestedArray = dataCache._nestedArray;
            _index = index;
        }

        private sealed class MultiValueFacetDocIdSetIterator : FacetFilter.FacetDocIdSetIterator
        {
            private readonly BigNestedIntArray _nestedArray;

            public MultiValueFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, int index)
                : base(dataCache, index)
            {
                _nestedArray = dataCache._nestedArray;
            }

            public override bool Next()
            {
                while (doc < maxID) // not yet reached end
                {
                    if (_nestedArray.Contains(++doc, index))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override bool SkipTo(int id)
            {

                if (id > doc)
                {
                    doc = id - 1;
                    return Next();
                }

                return Next();
            }
        }

        private class RandomRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private MultiValueFacetFilter parent;

            public RandomRandomAccessDocIdSet(MultiValueFacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new MultiValueFacetDocIdSetIterator(parent._dataCache, parent._index);
            }
            public override bool Get(int docId)
            {
                return parent._nestedArray.Contains(docId, parent._index);
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (_index < 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                return new RandomRandomAccessDocIdSet(this);
            }
        }
    }
}