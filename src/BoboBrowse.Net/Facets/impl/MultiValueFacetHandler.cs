using System;
using System.Collections.Generic;
using BoboBrowse.Api;
using BoboBrowse.Facets.data;
using BoboBrowse.Facets.filter;
using BoboBrowse.LangUtils;
using BoboBrowse.query.scoring;
using BoboBrowse.Util;
using C5;
using log4net;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.impl
{
    public class MultiValueFacetHandler : FacetHandler, IFacetHandlerFactory, IFacetScoreable
    {
        private static log4net.ILog logger = LogManager.GetLogger(typeof(MultiValueFacetHandler));

        public override ScoreDocComparator GetScoreDocComparator()
        {
            return new MultiValueFacetDataCache.MultiFacetScoreDocComparator(_dataCache);
        }

        private readonly TermListFactory _termListFactory;
        private readonly string _indexFieldName;

        private int _maxItems = BigNestedIntArray.MAX_ITEMS;
        protected internal MultiValueFacetDataCache _dataCache;
        private Term _sizePayloadTerm;
        protected internal List<string> _depends;

        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, Term sizePayloadTerm, List<string> depends)
            : base(name, depends)
        {
            _depends = depends;
            _indexFieldName = (indexFieldName != null ? indexFieldName : name);
            _termListFactory = termListFactory;
            _sizePayloadTerm = sizePayloadTerm;
            _dataCache = null;
        }

        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, Term sizePayloadTerm)
            : this(name, indexFieldName, termListFactory, sizePayloadTerm, null)
        {
        }

        public MultiValueFacetHandler(string name, TermListFactory termListFactory, Term sizePayloadTerm)
            : this(name, name, termListFactory, sizePayloadTerm, null)
        {
        }

        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : this(name, indexFieldName, termListFactory, null, null)
        {
        }

        public MultiValueFacetHandler(string name, TermListFactory termListFactory)
            : this(name, name, termListFactory)
        {
        }

        public MultiValueFacetHandler(string name, string indexFieldName)
            : this(name, indexFieldName, null)
        {
        }

        public MultiValueFacetHandler(string name)
            : this(name, name, null)
        {
        }

        public MultiValueFacetHandler(string name, List<string> depends)
            : this(name, name, null, null, depends)
        {
        }

        public virtual FacetHandler NewInstance()
        {
            return new MultiValueFacetHandler(Name, _indexFieldName, _termListFactory, _sizePayloadTerm, _depends);
        }

        public MultiValueFacetDataCache getDataCache()
        {
            return _dataCache;
        }

        public virtual void setMaxItems(int maxItems)
        {
            _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
        }

        public override string[] GetFieldValues(int id)
        {
            return _dataCache._nestedArray.getTranslatedData(id, _dataCache.valArray);
        }

        public override object[] GetRawFieldValues(int id)
        {
            return new object[] { _dataCache._nestedArray.getRawData(id, _dataCache.valArray) };
        }


        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec ospec)
        {
            return new MultiValueFacetCountCollector(sel, _dataCache, name, ospec);
        }

        public override void Load(BoboIndexReader reader)
        {
            Load(reader, new BoboIndexReader.WorkArea());
        }

        public override void Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            if (_dataCache == null)
            {
                _dataCache = new MultiValueFacetDataCache();
            }

            _dataCache.SetMaxItems(_maxItems);

            if (_sizePayloadTerm == null)
            {
                _dataCache.Load(_indexFieldName, reader, _termListFactory, workArea);
            }
            else
            {
                _dataCache.Load(_indexFieldName, reader, _termListFactory, _sizePayloadTerm);
            }
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties prop)
        {
            int index = _dataCache.valArray.IndexOf(@value);
            if (index >= 0)
                return new MultiValueFacetFilter(_dataCache, index);
            else
                return null;
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {

            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null)
                {
                    filterList.Add(f);
                }
                else
                {
                    return EmptyFilter.GetInstance();
                }
            }
            if (filterList.Count == 1)
                return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            RandomAccessFilter filter = null;

            int[] indexes = FacetDataCache.Convert(_dataCache, vals);
            if (indexes.Length > 1)
            {
                filter = new MultiValueORFacetFilter(_dataCache, indexes);
            }
            else if (indexes.Length == 1)
            {
                filter = new MultiValueFacetFilter(_dataCache, indexes[0]);
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

        public virtual BoboDocScorer GetDocScorer(IFacetTermScoringFunctionFactory scoringFunctionFactory, Dictionary<string, float> boostMap)
        {
            float[] boostList = BoboDocScorer.BuildBoostList(_dataCache.valArray.GetInnerList(), boostMap);
            return new MultiValueDocScorer(_dataCache, scoringFunctionFactory, boostList);
        }

        private sealed class MultiValueDocScorer : BoboDocScorer
        {
            private readonly MultiValueFacetDataCache _dataCache;
            private readonly BigNestedIntArray _array;

            internal MultiValueDocScorer(MultiValueFacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.valArray.Count, dataCache._nestedArray.size()), boostList)
            {
                _dataCache = dataCache;
                _array = _dataCache._nestedArray;
            }

            public override Explanation Explain(int doc)
            {
                string[] vals = _array.getTranslatedData(doc, _dataCache.valArray);

                ArrayList<float> scoreList = new ArrayList<float>(_dataCache.valArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                foreach (string val in vals)
                {
                    int idx = _dataCache.valArray.IndexOf(val);
                    if (idx >= 0)
                    {
                        scoreList.Add(Function.Score(_dataCache.freqs[idx], BoostList[idx]));
                        explList.Add(Function.Explain(_dataCache.freqs[idx], BoostList[idx]));
                    }
                }
                Explanation topLevel = Function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                return _array.getScores(docid, _dataCache.freqs, BoostList, Function);
            }

        }

        private sealed class MultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            private readonly new BigNestedIntArray _array;
            internal MultiValueFacetCountCollector(BrowseSelection sel, FacetDataCache dataCache, string name, FacetSpec ospec)
                : base(sel, dataCache, name, ospec)
            {
                _array = ((MultiValueFacetDataCache)(_dataCache))._nestedArray;
            }

            public override sealed void Collect(int docid)
            {
                _array.count(docid, _count);
            }

            public override sealed void CollectAll()
            {
                _count = _dataCache.freqs;
            }
        }
    }
}