using System;
using System.Linq;
using System.Collections.Generic;
using BoboBrowse.LangUtils;

namespace BoboBrowse.Api
{
	[Serializable]
	public class MappedFacetAccessible : IFacetAccessible
	{
		private const long serialVersionUID = 1L;

		private readonly Dictionary<object, BrowseFacet> facetMap;
		private readonly BrowseFacet[] facets;

		public MappedFacetAccessible(BrowseFacet[] facets)
		{
            this.facets = facets;

            facetMap = new Dictionary<object, BrowseFacet>();

			foreach (BrowseFacet facet in facets)
			{
				facetMap.Add(facet.Value, facet);
			}
		}

		public virtual BrowseFacet GetFacet(string @value)
		{
			return facetMap[@value];
		}

        public virtual IEnumerable<BrowseFacet> GetFacets()
		{
			return facets.ToList();
		}
	}
}