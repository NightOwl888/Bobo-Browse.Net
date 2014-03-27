using Lucene.Net.Search;

namespace BoboBrowse.Api
{
    public abstract class TopDocsSortedHitCollector : Collector
    {
        //public override void Collect(int doc, float score);
        // FIMXE: check whatever do we need additional methods or not

        public abstract BrowseHit[] GetTopDocs(); // throws IOException;
        public abstract int GetTotalHits();
    }
}