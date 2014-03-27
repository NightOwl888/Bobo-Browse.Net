using System.Collections.Generic;

namespace BoboBrowse.query.scoring
{
	public interface IFacetScoreable
	{
		 BoboDocScorer GetDocScorer(IFacetTermScoringFunctionFactory scoringFunctionFactory, Dictionary<string, float> boostMap);
	}
}