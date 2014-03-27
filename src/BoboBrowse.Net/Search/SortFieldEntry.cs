using System.Globalization;
using Lucene.Net.Search;

namespace BoboBrowse.Search
{
    /// <summary> Expert: Every composite-key in the internal cache is of this type.  </summary>
    public class SortFieldEntry
    {
        internal readonly string Field; // which Fieldable
        internal readonly int Type; // which SortField type
        internal readonly SortComparatorSource Custom; // which custom comparator
        internal readonly CultureInfo Locale; // the locale we're sorting (if string)

        /// <summary> Creates one of these objects.  </summary>
        public SortFieldEntry(string field, int type, CultureInfo locale)
        {
            Field = string.Intern(field);
            Type = type;
            Custom = null;
            Locale = locale;
        }

        /// <summary> Creates one of these objects for a custom comparator.  </summary>
        public SortFieldEntry(string field, SortComparatorSource custom)
        {
            Field = string.Intern(field);
            Type = SortField.CUSTOM;
            Custom = custom;
            Locale = null;
        }

        ///<summary>Two of these are equal iff they reference the same field and type.  </summary>
        public override bool Equals(object o)
        {
            if (o is SortFieldEntry)
            {
                SortFieldEntry other = (SortFieldEntry)o;
                if (other.Field == Field && other.Type == Type)
                {
                    if (other.Locale == null ? Locale == null :  other.Locale.Equals(Locale))
                    {
                        if (other.Custom == null)
                        {
                            if (Custom == null)
                            {
                                return true;
                            }
                        }
                        else if (other.Custom.Equals(Custom))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        ///<summary>Composes a hashcode based on the field and type.  </summary>
        public override int GetHashCode()
        {
            return Field.GetHashCode() ^ Type ^ (Custom == null ? 0 : Custom.GetHashCode()) ^ (Locale == null ? 0 : Locale.GetHashCode());
        }
    }
}