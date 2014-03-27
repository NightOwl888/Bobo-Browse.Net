using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets.data;
using BoboBrowse.Util;

namespace BoboBrowse.Facets.impl
{
    public abstract class DefaultFacetCountCollector : IFacetCountCollector
    {
        protected internal readonly FacetSpec _ospec;
        protected internal int[] _count;
        protected internal readonly FacetDataCache _dataCache;
        private readonly string _name;
        protected internal readonly BrowseSelection _sel;
        protected internal readonly BigSegmentedArray _array;

        public DefaultFacetCountCollector(BrowseSelection sel, FacetDataCache dataCache, string name, FacetSpec ospec)
        {
            _sel = sel;
            _ospec = ospec;
            _name = name;
            _dataCache = dataCache;
            _count = new int[_dataCache.freqs.Length];
            _array = _dataCache.orderArray;
        }

        public virtual string Name
        {
            get
            {
                return _name;
            }
        }

        public abstract void Collect(int docid);

        public abstract void CollectAll();

        public virtual BrowseFacet GetFacet(string @value)
        {
            BrowseFacet facet = null;
            int index = _dataCache.valArray.IndexOf(@value);
            if (index >= 0)
            {
                facet = new BrowseFacet(_dataCache.valArray.Get(index), _count[index]);
            }
            else
            {
                facet = new BrowseFacet(_dataCache.valArray.Format(@value), 0);
            }
            return facet;
        }

        public virtual int[] GetCountDistribution()
        {
            return _count;
        }

        private class DefaultFacetCountCollectorFieldAccessor : IFieldValueAccessor
        {
            private FacetDataCache _dataCache;

            public DefaultFacetCountCollectorFieldAccessor(FacetDataCache _dataCache)
            {
                this._dataCache = _dataCache;
            }

            public string GetFormatedValue(int index)
            {
                return _dataCache.valArray.Get(index);
            }

            public object GetRawValue(int index)
            {
                return _dataCache.valArray.GetRawValue(index);
            }
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (_ospec != null)
            {
                string facetPrefix = _ospec.Prefix;
                bool checkPrefix = !string.IsNullOrEmpty(facetPrefix);

                int minCount = _ospec.MinHitCount;
                int max = _ospec.MaxCount;
                if (max <= 0)
                {
                    max = _count.Length;
                }

                ICollection<BrowseFacet> facetColl;
                List<string> valList = _dataCache.valArray.GetInnerList();
                FacetSpec.FacetSortSpec sortspec = _ospec.OrderBy;
                if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                {
                    facetColl = new List<BrowseFacet>(max);
                    for (int i = 1; i < _count.Length; ++i) // exclude zero
                    {
                        int hits = _count[i];
                        if (!checkPrefix || valList[i].StartsWith(facetPrefix))
                        {
                            if (hits >= minCount)
                            {
                                BrowseFacet facet = new BrowseFacet(valList[i], hits);
                                facetColl.Add(facet);
                            }
                        }
                        if (facetColl.Count >= max)
                            break;
                    }
                }
                else //if (sortspec == FacetSortSpec.OrderHitsDesc)
                {
                    IComparatorFactory comparatorFactory;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                    {
                        comparatorFactory = new FacetHitcountComparatorFactory();
                    }
                    else
                    {
                        comparatorFactory = _ospec.CustomComparatorFactory;
                    }

                    if (comparatorFactory == null)
                    {
                        throw new System.ArgumentException("facet comparator factory not specified");
                    }

                    IComparer<int> comparator = comparatorFactory.NewComparator(new DefaultFacetCountCollectorFieldAccessor(_dataCache), _count);
                    facetColl = new LinkedList<BrowseFacet>();
                    BoundedPriorityQueue<int> pq = new BoundedPriorityQueue<int>(comparator, max);

                    for (int i = 1; i < _count.Length; ++i) // exclude zero
                    {
                        int hits = _count[i];
                        if (hits >= minCount)
                        {
                            if (!checkPrefix || valList[i].StartsWith(facetPrefix))
                            {
                                if (!pq.Offer(i))
                                {
                                    // pq is full. we can safely ignore any facet with <=hits.
                                    minCount = hits + 1;
                                }
                            }
                        }
                    }

                    while (!pq.IsEmpty)
                    {
                        int val = pq.DeleteMax();
                        BrowseFacet facet = new BrowseFacet(valList[val], _count[val]);
                        facetColl.Add(facet);
                    }
                }
                return facetColl;
            }
            else
            {
                return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
            }
        }
    }
}