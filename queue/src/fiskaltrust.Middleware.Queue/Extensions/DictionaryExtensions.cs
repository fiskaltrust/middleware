using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class DictionaryExtensions
    {
        public static bool GetBoolOrDefault(this IDictionary<string, object> dictionary, string key) 
            => dictionary.TryGetValue(key, out var val) && bool.TryParse(val.ToString(), out var boolVal) && boolVal;

        public static int GetIntOrDefault(this IDictionary<string, object> dictionary, string key, int defaultValue) 
            => dictionary.TryGetValue(key, out var val) && int.TryParse(val.ToString(), out var intVal) ? intVal : defaultValue;
    }
}
