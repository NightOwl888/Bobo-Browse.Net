using System.Collections.Generic;
using System.IO;
using BoboBrowse.Api;
using BoboBrowse.Facets.filter;
using BoboBrowse.LangUtils;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.impl
{
    public class FilteredRangeFacetHandler : FacetHandler, IFacetHandlerFactory
	{
		private readonly List<string> _predefinedRanges;
		private readonly string _inner;
		private RangeFacetHandler _innerHandler;

        public FilteredRangeFacetHandler(string name, string underlyingHandler, List<string> predefinedRanges)
            : base(name, new string[] { underlyingHandler })
        {
            _predefinedRanges = predefinedRanges;
            _inner = underlyingHandler;
            _innerHandler = null;
        }

	    public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties selectionProperty)
		{
			return _innerHandler.BuildRandomAccessFilter(@value, selectionProperty);
		}


		public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
		{
			return _innerHandler.BuildRandomAccessAndFilter(vals, prop);
		}

		public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
		{
			return _innerHandler.BuildRandomAccessOrFilter(vals, prop, isNot);
		}

		public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
		{
			return new RangeFacetCountCollector(name, _innerHandler.GetDataCache(), fspec, _predefinedRanges, false);
		}

		public override string[] GetFieldValues(int id)
		{
			return _innerHandler.GetFieldValues(id);
		}

		public override object[] GetRawFieldValues(int id)
		{
			return _innerHandler.GetRawFieldValues(id);
		}

		public override ScoreDocComparator GetScoreDocComparator()
		{
			return _innerHandler.GetScoreDocComparator();
		}

		public override void Load(BoboIndexReader reader)
		{
			FacetHandler handler = reader.GetFacetHandler(_inner);
			if (handler is RangeFacetHandler)
			{
				_innerHandler = (RangeFacetHandler)handler;
			}
			else
			{
				throw new IOException("inner handler is not instance of "+typeof(RangeFacetHandler));
			}
		}

		public virtual FacetHandler NewInstance()
		{
			return new FilteredRangeFacetHandler(name,_inner, _predefinedRanges);
		}
	}

}