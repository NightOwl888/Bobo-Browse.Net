namespace BoboBrowse.query.scoring
{
	public class DefaultFacetTermScoringFunctionFactory : IFacetTermScoringFunctionFactory
	{
		public virtual IFacetTermScoringFunction GetFacetTermScoringFunction(int termCount, int docCount)
		{
			return new DefaultFacetTermScoringFunction();
		}
	}
}