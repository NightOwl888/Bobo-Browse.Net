using System;
using System.Text;

namespace BoboBrowse.Api
{
    ///<summary>specifies how facets are to be returned for a browse</summary>
    [Serializable]
    public class FacetSpec
    {
        ///<summary>Sort options for facets </summary>
        public enum FacetSortSpec
        {
            ///<summary>Order by the facet values in lexographical ascending order </summary>
            OrderValueAsc,
            ///<summary>Order by the facet hit counts in descending order </summary>
            OrderHitsDesc,
            ///<summary>custom order, must have a comparator </summary>
            OrderByCustom
        }

        ///<summary>The minimum number of hits a choice would need to have to be returned. </summary>
        public int MinHitCount { get; set; }

        ///<summary>The maximum number of choices to return. Default = 0 which means all </summary>
        public int MaxCount { get; set; }

        ///<summary>Whether we are expanding sibling choices </summary>
        public bool ExpandSelection { get; set; }

        ///<summary>Get the current choice sort order </summary>
        public FacetSortSpec OrderBy { get; set; }

        public string Prefix { get; set; }

        public IComparatorFactory CustomComparatorFactory { get; set; }

        public FacetSpec()
        {
            OrderBy = FacetSortSpec.OrderValueAsc;
            MinHitCount = 1;
            ExpandSelection = false;
            CustomComparatorFactory = null;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("orderBy: ").Append(OrderBy).Append("\n");
            buffer.Append("max count: ").Append(MaxCount).Append("\n");
            buffer.Append("min hit count: ").Append(MinHitCount).Append("\n");
            buffer.Append("expandSelection: ").Append(ExpandSelection);
            return buffer.ToString();
        }
    }
}