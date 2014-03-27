using System;
using System.Collections.Generic;
using BoboBrowse.Facets.data;
using Lucene.Net.Util;
using System.Globalization;

namespace BoboBrowse.Facets.data
{
    public class TermDoubleList : TermValueList<object>
    {
        public override string Format(object @value)
        {
            return @value != null ? ((double)@value).ToString() : null;
        }

        private static object Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return null;
            }
            else
            {
                return NumericUtils.PrefixCodedToDouble(s);
            }
        }

        public override void Add(string @value)
        {
            Add(Parse(@value));
        }

        public override int IndexOf(object o)
        {
            double val = double.Parse((string)o, CultureInfo.InvariantCulture);
            return this.BinarySearch(val);
        }
    }

}