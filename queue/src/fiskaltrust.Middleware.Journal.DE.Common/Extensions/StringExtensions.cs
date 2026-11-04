using System.Linq;

namespace fiskaltrust.Middleware.Journal.DE.Common.Extensions
{
    public static class StringExtensions
    {
        public static string SafeSubstring(this string s, int from, int to)
        {
            if (s == null)
            {
                return string.Empty;
            }
            return new string(s.Skip(from).Take(to).ToArray()); 
        }
    }
}
