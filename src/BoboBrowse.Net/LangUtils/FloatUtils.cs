using System;

namespace BoboBrowse.LangUtils
{
    public static class FloatUtils 
    {
        public static int floatToIntBits(this float f) 
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
        }
    }
}
