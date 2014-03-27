using System.Text;

namespace com.browseengine.bobo.util
{


	using DocIdSet = Lucene.Net.Search.DocIdSet;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

	public class DocIdSetUtil
	{
	  private DocIdSetUtil()
	  {
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static String toString(DocIdSet docIdSet) throws IOException
	  public static string ToString(DocIdSet docIdSet)
	  {
          DocIdSetIterator iter = docIdSet.Iterator();
		StringBuilder buf = new StringBuilder();
		bool firstTime = true;
		buf.Append("[");
		while(iter.Current)
		{
		  if (firstTime)
		  {
			firstTime = false;
		  }
		  else
		  {
			buf.Append(",");
		  }
		  buf.Append(iter.doc());
		}
		buf.Append("]");
		return buf.ToString();
	  }
	}

}