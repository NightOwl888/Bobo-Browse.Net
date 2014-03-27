#region using

using System.Collections.Generic;
using System.Linq;

#endregion

namespace BoboBrowse.LangUtils
{
    public static class Arrays
    {
        public static void Fill<T>(T[] block, T value)
        {
            for (int i = 0; i < block.Length; i++)
            {
                block[i] = value;
            }
        }

        public static string ToString<T>(T[] block)
        {
            return string.Join(", ", block.Select(x => x.ToString()).ToArray());
        }
    }
}