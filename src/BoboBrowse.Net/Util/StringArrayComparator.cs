using System;

namespace BoboBrowse.Util
{
    public class StringArrayComparator : IComparable<StringArrayComparator>, IComparable
    {
        private readonly string[] vals;

        public StringArrayComparator(string[] vals)
        {
            this.vals = vals;
        }

        public virtual int CompareTo(StringArrayComparator node)
        {
            string[] o = node.vals;
            if (vals == o)
            {
                return 0;
            }
            if (vals == null)
            {
                return -1;
            }
            if (o == null)
            {
                return 1;
            }
            for (int i = 0; i < vals.Length; ++i)
            {
                if (i >= o.Length)
                {
                    return 1;
                }
                int compVal = vals[i].CompareTo(o[i]);
                if (compVal != 0)
                {
                    return compVal;
                }
            }
            if (vals.Length == o.Length)
            {
                return 0;
            }
            return -1;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((StringArrayComparator) obj);
        }
    }
}