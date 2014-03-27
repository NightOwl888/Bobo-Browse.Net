using System;
using System.Collections.Generic;
using BoboBrowse.Facets.data;
using Lucene.Net.Util;
using System.Globalization;

namespace BoboBrowse.Facets.data
{
    public class TermFloatList : TermValueList<object>
    {
        public override string Format(object @value)
        {
            return @value != null ? ((float)@value).ToString() : null;
        }

        private static object Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return null;
            }
            else
            {
                return NumericUtils.PrefixCodedToFloat(s);
            }
        }

        public override void Add(string @value)
        {
            Add(Parse(@value));
        }

        public override int IndexOf(object o)
        {
            float val = float.Parse((string)o, CultureInfo.InvariantCulture);
            return this.BinarySearch(val);
        }
    }

}