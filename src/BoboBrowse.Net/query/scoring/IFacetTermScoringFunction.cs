using Lucene.Net.Search;

namespace BoboBrowse.query.scoring
{
    public interface IFacetTermScoringFunction
	{
		void ClearScores();
		float Score(int df, float boost);
		void ScoreAndCollect(int df, float boost);
        float GetCurrentScore();
        Explanation Explain(int df, float boost);
        Explanation Explain(params float[] scores);
    }
}