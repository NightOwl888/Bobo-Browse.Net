using Lucene.Net.Search;

namespace BoboBrowse.docidset
{
    public abstract class RandomAccessDocIdSet : DocIdSet
	{
	  public abstract bool Get(int docId);
	}
}