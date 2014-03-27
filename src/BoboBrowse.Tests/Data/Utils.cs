using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace BoboBrowse.Tests.Data
{
    internal class Utils
    {
        internal static string GetTestData(string location)
        {
            string codeBaseStr = Assembly.GetCallingAssembly().CodeBase;
            var codeBaseUri = new Uri(codeBaseStr);

            return Path.Combine(Path.GetDirectoryName(codeBaseUri.LocalPath), @"..\..\Data\" + location);
        }
    }
}
