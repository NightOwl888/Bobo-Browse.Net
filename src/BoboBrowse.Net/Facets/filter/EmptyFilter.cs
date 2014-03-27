using BoboBrowse.docidset;
using Lucene.Net.Index;

namespace BoboBrowse.Facets.filter
{
    public class EmptyFilter : RandomAccessFilter
    {
        private readonly static EmptyFilter instance = new EmptyFilter();

        private EmptyFilter()
        {
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            return EmptyDocIdSet.GetInstance();
        }

        public static EmptyFilter GetInstance()
        {
            return instance;
        }
    }
}