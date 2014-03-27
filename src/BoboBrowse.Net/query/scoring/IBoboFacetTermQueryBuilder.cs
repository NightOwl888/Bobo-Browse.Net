using Lucene.Net.Search;

namespace BoboBrowse.query.scoring
{
    public interface IBoboFacetTermQueryBuilder
	{
		Query BuildFacetTermQuery(IFacetTermScoringFunctionFactory scoreFunctionFactory);
	}
}