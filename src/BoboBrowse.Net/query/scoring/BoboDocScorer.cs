using System.Collections.Generic;
using BoboBrowse.LangUtils;
using Lucene.Net.Search;

namespace BoboBrowse.query.scoring
{
    public abstract class BoboDocScorer
    {
        protected internal readonly IFacetTermScoringFunction Function;
        protected internal readonly float[] BoostList;

        protected BoboDocScorer(IFacetTermScoringFunction scoreFunction, float[] boostList)
        {
            Function = scoreFunction;
            BoostList = boostList;
        }

        public abstract float Score(int docid);

        public abstract Explanation Explain(int docid);

        public static float[] BuildBoostList(List<string> valArray, Dictionary<string, float> boostMap)
        {
            float[] boostList = new float[valArray.Count];
            Arrays.Fill(boostList, 1.0f);
            if (boostMap != null && boostMap.Count > 0)
            {
                Dictionary<string, float>.Enumerator iter = boostMap.GetEnumerator();
                while (iter.MoveNext())
                {
                    KeyValuePair<string, float> entry = iter.Current;
                    int index = valArray.IndexOf(entry.Key);
                    if (index >= 0)
                    {
                        float fval = entry.Value;
                        if (fval != null)
                        {
                            boostList[index] = fval;
                        }
                    }
                }
            }
            return boostList;
        }
    }
}