using System.Collections.Generic;
using BoboBrowse.Facets;
using Lucene.Net.Search;

namespace BoboBrowse.Api
{
    public interface IBrowsable : Searchable
	{
        void Browse(BrowseRequest req, Collector hitCollector, Dictionary<string, IFacetAccessible> facets); // throws BrowseException;

        BrowseResult Browse(BrowseRequest req); // throws BrowseException;

        void SetFacetHandler(FacetHandler facetHandler); // throws IOException;

		FacetHandler GetFacetHandler(string name);

		Similarity GetSimilarity();

		void SetSimilarity(Similarity similarity);

        string[] GetFieldVal(int docid, string fieldname); // throws IOException;

        object[] GetRawFieldVal(int docid, string fieldname); // throws IOException;

		int NumDocs();

        Explanation Explain(Query q, int docid); // throws IOException;

		TopDocsSortedHitCollector GetSortedHitCollector(SortField[] sort, int offset, int count, bool fetchStoredFields);
	}
}