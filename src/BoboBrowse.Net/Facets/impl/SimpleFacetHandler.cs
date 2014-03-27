using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets.data;
using BoboBrowse.Facets.filter;
using BoboBrowse.LangUtils;
using BoboBrowse.query.scoring;
using log4net;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.impl
{
    public class SimpleFacetHandler : FacetHandler, IFacetHandlerFactory, IFacetScoreable
    {
        private static log4net.ILog logger = LogManager.GetLogger(typeof(SimpleFacetHandler));
        private FacetDataCache _dataCache;
        private readonly TermListFactory _termListFactory;
        private readonly string _indexFieldName;

        public SimpleFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : base(name)
        {
            _indexFieldName = indexFieldName;
            _dataCache = null;
            _termListFactory = termListFactory;
        }

        public SimpleFacetHandler(string name, TermListFactory termListFactory)
            : this(name, name, termListFactory)
        {
        }

        public SimpleFacetHandler(string name)
            : this(name, name, null)
        {
        }

        public SimpleFacetHandler(string name, string indexFieldName)
            : this(name, indexFieldName, null)
        {
        }

        public virtual FacetHandler NewInstance()
        {
            return new SimpleFacetHandler(Name, _indexFieldName, _termListFactory);
        }

        public override ScoreDocComparator GetScoreDocComparator()
        {
            return _dataCache.GetScoreDocComparator();
        }

        public override string[] GetFieldValues(int id)
        {
            return new string[] { _dataCache.valArray.Get(_dataCache.orderArray.Get(id)) };
        }

        public override object[] GetRawFieldValues(int id)
        {
            return new object[] { _dataCache.valArray.GetRawValue(_dataCache.orderArray.Get(id)) };
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties prop)
        {
            int index = _dataCache.valArray.IndexOf(@value);
            if (index >= 0)
                return new FacetFilter(_dataCache, index);
            else
                return null;
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            if (vals.Length > 1)
            {
                return EmptyFilter.GetInstance();
            }
            else
            {
                return BuildRandomAccessFilter(vals[0], prop);
            }
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            RandomAccessFilter filter = null;

            int[] indexes = FacetDataCache.Convert(_dataCache, vals);
            if (indexes.Length > 1)
            {
                return new FacetOrFilter(_dataCache, indexes, isNot);
            }
            else if (indexes.Length == 1)
            {
                filter = new FacetFilter(_dataCache, indexes[0]);
            }
            else
            {
                filter = EmptyFilter.GetInstance();
            }
            if (isNot)
            {
                filter = new RandomAccessNotFilter(filter);
            }
            return filter;
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec ospec)
        {
            return new SimpleFacetCountCollector(sel, _dataCache, name, ospec);
        }

        public FacetDataCache getDataCache()
        {
            return _dataCache;
        }

        public override void Load(BoboIndexReader reader)
        {
            if (_dataCache == null)
            {
                _dataCache = new FacetDataCache();
            }
            _dataCache.Load(_indexFieldName, reader, _termListFactory);
        }

        public virtual BoboDocScorer GetDocScorer(IFacetTermScoringFunctionFactory scoringFunctionFactory, Dictionary<string, float> boostMap)
        {
            float[] boostList = BoboDocScorer.BuildBoostList(_dataCache.valArray.GetInnerList(), boostMap);
            return new SimpleBoboDocScorer(_dataCache, scoringFunctionFactory, boostList);
        }

        public sealed class SimpleFacetCountCollector : DefaultFacetCountCollector
        {
            public SimpleFacetCountCollector(BrowseSelection sel, FacetDataCache dataCache, string name, FacetSpec ospec)
                : base(sel, dataCache, name, ospec)
            {
            }

            public override void Collect(int docid)
            {
                _count[_array.Get(docid)]++;
            }

            public override void CollectAll()
            {
                _count = _dataCache.freqs;
            }
        }

        public sealed class SimpleBoboDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache _dataCache;

            public SimpleBoboDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.valArray.Count, dataCache.orderArray.Size()), boostList)
            {
                _dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int idx = _dataCache.orderArray.Get(doc);
                return Function.Explain(_dataCache.freqs[idx], BoostList[idx]);
            }

            public override sealed float Score(int docid)
            {
                int idx = _dataCache.orderArray.Get(docid);
                return Function.Score(_dataCache.freqs[idx], BoostList[idx]);
            }
        }
    }
}