using System.Collections.Generic;
using BoboBrowse.Api;
using System.Collections;

namespace BoboBrowse.Facets.impl
{
    public class FacetHitcountComparatorFactory : IComparatorFactory
    {
        private class FacetHitComparer : IComparer<int>
        {
            internal int[] counts;

            public int Compare(int f1, int f2)
            {
                int val = counts[f1] - counts[f2];
                if (val == 0)
                {
                    val = f2 - f1;
                }
                return val;
            }
        }

        public virtual IComparer<int> NewComparator(IFieldValueAccessor valueList, int[] counts)
        {
            return new FacetHitComparer { counts = counts };
        }

        private class FACETIComparer : IComparer<BrowseFacet>
        {
            // FIXME: we need to reorganize all that stuff with comparators
            private IComparer valueComparer = new Comparer(System.Globalization.CultureInfo.InvariantCulture);

            public int Compare(BrowseFacet f1, BrowseFacet f2)
            {
                int val = f1.HitCount - f2.HitCount;
                if (val == 0)
                {
                    val = valueComparer.Compare(f1.Value, f2.Value);
                }
                return val;
            }
        }

        public static IComparer<BrowseFacet> FACET_HITS_COMPARATOR = new FACETIComparer();

        public IComparer<BrowseFacet> NewComparator()
        {
            return FACET_HITS_COMPARATOR;
        }
    }
}