using System;
using System.Collections.Generic;
using System.Reflection;

namespace BoboBrowse.Facets.data
{
    /// <summary> * Class supported:
    /// * <ul>
    /// *   <li> <seealso cref="Integer"/> </li>
    /// *   <li> <seealso cref="Float"/> </li>
    /// *   <li> <seealso cref="Character"/> </li>
    /// *   <li> <seealso cref="Double"/> </li>
    /// *   <li> <seealso cref="Short"/> </li>
    /// *   <li> <seealso cref="Long"/> </li>
    /// *   <li> <seealso cref="Date"/> </li>
    /// * </ul>
    /// * Autoboxing: primitive types corresponding classes above are supported. </summary>
    public class PredefinedTermListFactory<T> 
        : TermListFactory
    {
        private static Dictionary<Type, Type> supportedTypes = new Dictionary<Type, Type>();

        static PredefinedTermListFactory()
        {
            supportedTypes.Add(typeof(string), typeof(TermStringList));
            supportedTypes.Add(typeof(int), typeof(TermIntList));
            supportedTypes.Add(typeof(DateTime), typeof(TermDateList));
            supportedTypes.Add(typeof(float), typeof(TermFloatList));
            supportedTypes.Add(typeof(double), typeof(TermDoubleList));
            supportedTypes.Add(typeof(long), typeof(TermLongList));

            /*
            supportedTypes.Add(typeof(char), typeof(TermCharList));
            supportedTypes.Add(typeof(short), typeof(TermShortList));
            */
        }

        private readonly string format;

        public PredefinedTermListFactory()
            : this(null)
        {
        }

        public PredefinedTermListFactory(string format)
        {
            if (!supportedTypes.ContainsKey(typeof(T)))
            {
                throw new ArgumentException("Class " + typeof(T) + " not defined.");
            }
            this.format = format;
        }

        public override ITermValueList CreateTermList()
        {
            Type listClass = supportedTypes[typeof(T)];

            try
            {
                if (string.IsNullOrEmpty(format))
                {
                    ConstructorInfo constructor = listClass.GetConstructor(new Type[] { });
                    return (ITermValueList)constructor.Invoke(new object[] { });
                }
                else
                {
                    ConstructorInfo constructor = listClass.GetConstructor(new Type[] { typeof(string) });
                    return (ITermValueList)constructor.Invoke(new object[] { format });
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }
        }
    }
}