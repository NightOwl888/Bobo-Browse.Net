using System;
using System.Collections.Generic;

namespace com.browseengine.bobo.facets.data
{

	using CharArrayList = List<char>;


	public class TermCharList : TermValueList
	{

		private static char parse(string s)
		{
			return s==null ? (char)0 : s[0];
		}

		public TermCharList() : base()
		{
		}

		public TermCharList(int capacity) : base(capacity)
		{
		}

		public override bool add(string o)
		{
			return ((CharArrayList)_innerList).Add(parse(o));
		}

		protected internal override List<?> buildPrimitiveList(int capacity)
		{
			return capacity>0 ? new CharArrayList(capacity) : new CharArrayList();
		}

		public override int indexOf(object o)
		{
			char val =parse((string)o);
			char[] elements =((CharArrayList)_innerList).elements();
			return System.Array.BinarySearch(elements, val);
		}

		public override void seal()
		{
			((CharArrayList)_innerList).Trim();
		}

		public override string format(object o)
		{
			return Convert.ToString(o);
		}
	}

}