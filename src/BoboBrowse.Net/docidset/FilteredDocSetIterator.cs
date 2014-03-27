using Lucene.Net.Search;

namespace BoboBrowse.docidset
{
    public abstract class FilteredDocSetIterator : DocIdSetIterator
    {
        protected internal DocIdSetIterator innerIter;
        private int currentDoc;

        protected FilteredDocSetIterator(DocIdSetIterator innerIter)
        {
            if (innerIter == null)
            {
                throw new System.ArgumentException("null iterator");
            }
            this.innerIter = innerIter;
            currentDoc = -1;
        }

        protected internal abstract bool Match(int doc);

        public override int Doc()
        {
            return currentDoc;
        }

        public override bool Next()
        {
            while (innerIter.Next())
            {
                int doc = innerIter.Doc();
                if (Match(doc))
                {
                    currentDoc = doc;
                    return true;
                }
            }
            return false;
        }

        public override bool SkipTo(int n)
        {
            bool flag = innerIter.SkipTo(n);
            if (flag)
            {
                int doc = innerIter.Doc();
                if (Match(doc))
                {
                    currentDoc = doc;
                    return true;
                }
                else
                {
                    while (innerIter.Next())
                    {
                        int docid = innerIter.Doc();
                        if (Match(docid))
                        {
                            currentDoc = docid;
                            return true;
                        }
                    }
                    return false;
                }
            }
            return flag;
        }
    }
}