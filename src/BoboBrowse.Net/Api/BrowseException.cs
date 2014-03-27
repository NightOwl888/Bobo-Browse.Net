using System;

namespace BoboBrowse.Api
{
	public class BrowseException : Exception
	{
		public BrowseException(string msg) : this(msg,null)
		{
		}

		public BrowseException(string msg, System.Exception cause) : base(msg,cause)
		{
		}
	}
}