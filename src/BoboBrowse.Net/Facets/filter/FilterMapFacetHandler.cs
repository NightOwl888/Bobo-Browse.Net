using System;
using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.docidset;
using BoboBrowse.LangUtils;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.filter
{
    public class FilterMapFacetHandler : FacetHandler
    {
        protected internal readonly Dictionary<string, FacetEntry> _filterMap;
        protected internal readonly FacetEntry[] _facetEntries;
        protected internal BoboIndexReader _reader;

        public FilterMapFacetHandler(string name, Dictionary<string, RandomAccessFilter> filterMap)
            : base(name)
        {
            _facetEntries = new FacetEntry[filterMap.Count];
            _filterMap = new Dictionary<string, FacetEntry>();
            int i = 0;
            foreach (KeyValuePair<string, RandomAccessFilter> entry in filterMap)
            {
                FacetEntry f = new FacetEntry();
                f.filter = entry.Value;
                f.value = entry.Key;
                _facetEntries[i] = f;
                _filterMap.Add(f.value, f);
                i++;
            }
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties props)
        {
            return _filterMap[@value].filter;
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
        {
            return new FilterMapFacetCountCollector(this);
        }

        public override string[] GetFieldValues(int id)
        {
            List<string> values = new List<string>();
            foreach (FacetEntry entry in _facetEntries)
            {
                if (entry.docIdSet.Get(id))
                    values.Add(entry.value);
            }
            return values.Count > 0 ? values.ToArray() : null;
        }

        public override ScoreDocComparator GetScoreDocComparator()
        {
            return null;
        }

        public override void Load(BoboIndexReader reader)
        {
            _reader = reader;
            foreach (FacetEntry entry in _facetEntries)
                entry.docIdSet = entry.filter.GetRandomAccessDocIdSet(reader);
        }

        protected internal class FacetEntry
        {
            internal string @value;
            internal RandomAccessFilter filter;
            internal RandomAccessDocIdSet docIdSet;
        }

        protected internal class FilterMapFacetCountCollector : IFacetCountCollector
        {
            private FilterMapFacetHandler parent;
            private int[] _counts;

            public FilterMapFacetCountCollector(FilterMapFacetHandler parent)
            {
                this.parent = parent;
                _counts = new int[parent._facetEntries.Length];
            }

            public virtual int[] GetCountDistribution()
            {
                return _counts;
            }

            public virtual void Collect(int docid)
            {
                for (int i = 0; i < parent._facetEntries.Length; i++)
                {
                    if (parent._facetEntries[i].docIdSet.Get(docid))
                        _counts[i]++;
                }
            }

            public virtual void CollectAll()
            {
                throw new InvalidOperationException("not supported");
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                List<BrowseFacet> facets = new List<BrowseFacet>();
                for (int i = 0; i < parent._facetEntries.Length; i++)
                {
                    FacetEntry entry = parent._facetEntries[i];
                    BrowseFacet facet = new BrowseFacet();
                    facet.HitCount = _counts[i];
                    facet.Value = entry.value;
                    facets.Add(facet);
                }
                return facets;
            }

            public virtual string Name
            {
                get
                {
                    return parent.Name;
                }
            }

            public virtual List<BrowseFacet> combine(BrowseFacet facet, List<BrowseFacet> facets)
            {
                // TODO Auto-generated method stub
                return null;
            }

            public virtual BrowseFacet GetFacet(string @value)
            {
                // TODO Auto-generated method stub
                return null;
            }

        }

        public override object[] GetRawFieldValues(int id)
        {
            return GetFieldValues(id);
        }
    }
}