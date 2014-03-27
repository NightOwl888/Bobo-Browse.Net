using BoboBrowse.docidset;
using BoboBrowse.Facets;
using Lucene.Net.Search;

namespace BoboBrowse.Search
{
    public sealed class FacetHitCollector
	{
		public IFacetCountCollector FacetCountCollector;
		public DocIdSetIterator PostDocIDSetIterator;
		public int Doc;
		public bool More;
		public FacetHandler FacetHandler;
		public RandomAccessDocIdSet DocIdSet;
	}
}