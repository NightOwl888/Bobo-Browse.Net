namespace BoboBrowse.query.scoring
{
	public interface IFacetTermScoringFunctionFactory
	{
		IFacetTermScoringFunction GetFacetTermScoringFunction(int termCount, int docCount);
	}
}