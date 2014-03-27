using System.Collections.Generic;

namespace BoboBrowse.LangUtils
{
    public class Properties : Dictionary<string, string>
    {
        public string GetProperty(string key)
        {
            string result;

            return TryGetValue(key, out result) ? result : string.Empty;
        }
        public void SetProperty(string key, string value)
        {
            this[key] = value;
        }
    }
}
