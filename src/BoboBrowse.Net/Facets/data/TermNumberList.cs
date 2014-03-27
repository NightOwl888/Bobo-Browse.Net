using System;

namespace BoboBrowse.Facets.data
{
	public abstract class TermNumberList : TermValueList
	{

		private ThreadLocal<DecimalFormat> _formatter = null;
		protected internal string _formatString = null;

		protected internal TermNumberList() : base()
		{
		}

		protected internal TermNumberList(string formatString) : base()
		{
			setFormatString(formatString);
		}

		protected internal TermNumberList(int capacity, string formatString) : base(capacity)
		{
			setFormatString(formatString);
		}

		private void setFormatString(string formatString)
		{
			_formatString=formatString;
			_formatter = new ThreadLocal<DecimalFormat>()
			{
				  protected DecimalFormat initialValue()
				  {
					if (_formatString!=null)
					{
					  return new DecimalFormat(_formatString);
					}
					else
					{
					  return null;
					}

				  }
				}
		}

		public virtual string getFormatString()
		{
			return _formatString;
		}

		protected internal abstract object parseString(string o);

		public override string format(object o)
		{
			if (o == null)
				return null;
			if (o is string)
			{
				o = parseString((string)o);
			}
			if (_formatter == null)
			{
				return Convert.ToString(o);
			}
			else
			{
				DecimalFormat formatter =_formatter.get();
				if (formatter==null)
					return Convert.ToString(o);
				return _formatter.get().format(o);
			}
		}
	}

}