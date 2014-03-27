using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets.filter;
using BoboBrowse.LangUtils;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.impl
{
    ///<summary>@author ymatsuda
    ///</summary>
    public abstract class DynamicRangeFacetHandler : FacetHandler, IFacetHandlerFactory
    {
        protected internal readonly string dataFacetName;
        protected internal RangeFacetHandler dataFacetHandler;

        public DynamicRangeFacetHandler(string name, string dataFacetName)
            : base(name, new string[] { dataFacetName })
        {
            this.dataFacetName = dataFacetName;
        }

        protected internal abstract string buildRangeString(string val);
        protected internal abstract List<string> buildAllRangeStrings();
        protected internal abstract string getValueFromRangeString(object rangeString);
        public abstract FacetHandler NewInstance();

        public override RandomAccessFilter BuildRandomAccessFilter(string val, Properties props)
        {
            return dataFacetHandler.BuildRandomAccessFilter(buildRangeString(val), props);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(buildRangeString(val));
            }

            return dataFacetHandler.BuildRandomAccessAndFilter(valList.ToArray(), prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(buildRangeString(val));
            }
            return dataFacetHandler.BuildRandomAccessOrFilter(valList.ToArray(), prop, isNot);
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
        {
            List<string> list = buildAllRangeStrings();
            return new DynamicRangeFacetCountCollector(this, Name, dataFacetHandler, fspec, list);
        }

        public override string[] GetFieldValues(int docid)
        {
            return dataFacetHandler.GetFieldValues(docid);
        }

        public override ScoreDocComparator GetScoreDocComparator()
        {
            return dataFacetHandler.GetScoreDocComparator();
        }

        public override void Load(BoboIndexReader reader)
        {
            dataFacetHandler = (RangeFacetHandler)GetDependedFacetHandler(dataFacetName);
        }

        private class DynamicRangeFacetCountCollector : RangeFacetCountCollector
        {
            private DynamicRangeFacetHandler parent;

            internal DynamicRangeFacetCountCollector(DynamicRangeFacetHandler parent, string name, RangeFacetHandler handler, FacetSpec fspec, List<string> predefinedList)
                : base(name, handler, fspec, predefinedList, false)
            {
                this.parent = parent;
            }

            public override BrowseFacet GetFacet(string @value)
            {
                string rangeString = parent.buildRangeString(@value);
                BrowseFacet facet = base.GetFacet(rangeString);
                if (facet != null)
                {
                    return new BrowseFacet(@value, facet.HitCount);
                }
                else
                {
                    return null;
                }
            }

            public override IEnumerable<BrowseFacet> GetFacets()
            {
                IEnumerable<BrowseFacet> list = base.GetFacets();
                List<BrowseFacet> retList = new List<BrowseFacet>();
                IEnumerator<BrowseFacet> iter = list.GetEnumerator();
                while (iter.MoveNext())
                {
                    BrowseFacet facet = iter.Current;
                    object val = facet.Value;
                    string rangeString = parent.getValueFromRangeString(val);
                    if (rangeString != null)
                    {
                        BrowseFacet convertedFacet = new BrowseFacet(rangeString, facet.HitCount);
                        retList.Add(convertedFacet);
                    }
                }
                return retList;
            }
        }
    }
}