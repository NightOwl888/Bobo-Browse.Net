using System.IO;
using log4net;
using Lucene.Net.Search;
using LuceneExt.Api;

namespace LuceneExt.Impl
{
    public abstract class ImmutableDocSet : DocSet
    {
        private int size = -1;

        private static log4net.ILog log = LogManager.GetLogger(typeof(ImmutableDocSet));

        public override void AddDoc(int docid)
        {
            throw new System.NotSupportedException("Attempt to add document to an immutable data structure");

        }

        public override int Size()
        {
            // Do the size if we haven't done it so far.
            if (size < 0)
            {
                DocIdSetIterator dcit = Iterator();
                size = 0;
                try
                {
                    while (dcit.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        size++;
                    }
                }
                catch (IOException e)
                {
                    log.Error("Error computing size..");
                    return -1;
                }
            }
            return size;
        }
    }
}