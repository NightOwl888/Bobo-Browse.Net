using System;
using System.Linq;
using System.Collections.Generic;

namespace BoboBrowse.Facets.data
{
    public interface ITermValueList
    {
        int Count { get; }
        string Get(int index);
        object GetRawValue(int index);
        string Format(object o);
        int IndexOf(object o);
        void Add(string o);
        List<string> GetInnerList();
        void Seal();
    }

    /// <summary>This class behaves as List<String> with a few extensions:
    /// <ul>
    /// <li> Semi-immutable, e.g. once added, cannot be removed. </li>
    /// <li> Assumes sequence of values added are in sorted order </li>
    /// <li> <seealso cref="#indexOf(Object)"/> return value conforms to the contract of <seealso cref="Arrays#binarySearch(Object[], Object)"/></li>
    /// <li> <seealso cref="#seal()"/> is introduce to trim the List size, similar to <seealso cref="ArrayList#TrimToSize()"/>, once it is called, no add should be performed.</li>
    /// </u> </summary>
    public abstract class TermValueList<T> 
        : List<T>
        , ITermValueList
	{
		public abstract string Format(object o);
        public abstract void Add(string o);
        public abstract int IndexOf(object o);

		protected internal TermValueList()
		{
		}

		protected internal TermValueList(int capacity)
            : base(capacity)
		{
		}

        public virtual List<string> GetInnerList()
        {
            return new List<string>(this.Select(x => Format(x)));
        }

		public virtual bool Contains(object o)
		{
			return base.IndexOf((T)o)>=0;
		}

		public virtual string Get(int index)
		{
			return Format(this[index]);
		}

		public virtual object GetRawValue(int index)
		{
            return this[index];
		}

		public virtual bool IsEmpty()
		{
			return Count == 0;
		}

		public virtual int LastIndexOf(object o)
		{
			return base.IndexOf((T)o); // FIXME
		}

		public virtual int Size()
		{
			return Count;
		}

		public virtual List<string> SubList(int fromIndex, int toIndex)
		{
			throw new InvalidOperationException("not supported");
		}

        public virtual void Seal()
        {
            TrimExcess();
        }
    }
}