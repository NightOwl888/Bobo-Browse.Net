using System;
using System.Collections.Generic;

namespace com.browseengine.bobo.facets.data
{

	using ShortArrayList = it.unimi.dsi.fastutil.shorts.ShortArrayList;


	public class TermShortList : TermNumberList
	{
		private static short parse(string s)
		{
			if (s==null || s.Length == 0)
			{
				return (short)0;
			}
			else
			{
				return Convert.ToInt16(s);
			}
		}

		public TermShortList() : base()
		{
		}

		public TermShortList(string formatString) : base(formatString)
		{
		}

		public TermShortList(int capacity, string formatString) : base(capacity, formatString)
		{
		}

		public override bool add(string o)
		{
			return ((ShortArrayList)_innerList).Add(parse(o));
		}

//JAVA TO VB & C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: protected List<?> buildPrimitiveList(int capacity)
		protected internal override List<?> buildPrimitiveList(int capacity)
		{
			return capacity>0 ? new ShortArrayList(capacity) : new ShortArrayList();
		}

		public override int indexOf(object o)
		{
			short val =parse((string)o);
			short[] elements =((ShortArrayList)_innerList).elements();
			return System.Array.BinarySearch(elements, val);
		}

		public override void seal()
		{
			((ShortArrayList)_innerList).Trim();
		}

		protected internal override object parseString(string o)
		{
			return parse(o);
		}
	}

}