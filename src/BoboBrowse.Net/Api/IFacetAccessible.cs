using System.Collections.Generic;

namespace BoboBrowse.Api
{
	public interface IFacetAccessible
	{
        ///<summary>Gets gathered top facets </summary>
        ///<returns>list of facets </returns>
        IEnumerable<BrowseFacet> GetFacets();

        ///<summary>Gets the facet given a value. This is a way for random accessing into the facet data structure. </summary>
        ///<param name="value">Facet value </param>
        ///<returns>a facet with count filled in </returns>
		BrowseFacet GetFacet(string value);
	}
}