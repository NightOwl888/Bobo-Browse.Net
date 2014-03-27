using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BoboBrowse.Api
{
    ///<summary>This class represents a facet </summary>
    [Serializable]
    public class BrowseFacet
    {
        public int HitCount { get; set; }

        public object Value { get; set; }

        public BrowseFacet()
        {
        }

        public BrowseFacet(object @value, int hitCount)
        {
            Value = @value;
            HitCount = hitCount;
        }

        public override string ToString()
        {
            return string.Concat(Value, "(", HitCount, ")");
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is BrowseFacet)
            {
                BrowseFacet c2 = (BrowseFacet)obj;
                if (HitCount == c2.HitCount && Value.Equals(c2.Value))
                {
                    equals = true;
                }
            }
            return equals;
        }

        public virtual List<BrowseFacet> Merge(List<BrowseFacet> v, IComparer<BrowseFacet> comparator)
        {
            int i = 0;
            foreach (BrowseFacet facet in v)
            {
                int val = comparator.Compare(this, facet);
                if (val == 0)
                {
                    facet.HitCount += HitCount;
                    return v;
                }
                if (val > 0)
                {
                }
                i++;
            }
            v.Add(this);
            return v;
        }
    }
}