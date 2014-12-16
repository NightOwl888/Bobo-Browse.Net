﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;

    public class MultiplicativeFacetTermScoringFunction : IFacetTermScoringFunction
    {
        private float _boost = 1.0f;

        public sealed void ClearScores()
        {
            _boost = 1.0f;
        }

        public sealed float Score(int df, float boost)
        {
            return boost;
        }

        public sealed void ScoreAndCollect(int df, float boost)
        {
            if (boost > 0)
            {
                _boost *= boost;
            }
        }

        public sealed float GetCurrentScore()
        {
            return _boost;
        }

        public Explanation Explain(int df, float boost)
        {
            Explanation expl = new Explanation();
            expl.Value = Score(df, boost);
            expl.Description = "boost value of: " + boost;
            return expl;
        }

        public Explanation Explain(params float[] scores)
        {
            Explanation expl = new Explanation();
            float boost = 1.0f;
            foreach (float score in scores)
            {
                boost *= score;
            }
            expl.Value = boost;
            expl.Description = "product of: " + Arrays.ToString(scores);
            return expl;
        }
    }
}