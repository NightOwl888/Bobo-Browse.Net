using BoboBrowse.docidset;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace BoboBrowse.Facets.filter
{
    public abstract class RandomAccessFilter : Filter
    {
        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            return GetRandomAccessDocIdSet(reader);
        }

        public abstract RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader);
    }
}