using System;
using System.Collections.Generic;
using Lucene.Net.Util;

namespace BoboBrowse.Facets.data
{
	public class TermIntList : TermValueList<object>
	{
        public override string Format(object @value)
        {
            return @value != null ? ((int)@value).ToString() : null;
        }

        private static object Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return null;
            }
            else
            {
                return NumericUtils.PrefixCodedToInt(s);
            }
        }

		public override void Add(string @value)
		{
            Add(Parse(@value));
		}

        public override int IndexOf(object o)
		{
			int val = int.Parse((string)o);
			return this.BinarySearch(val);
		}
	}
}