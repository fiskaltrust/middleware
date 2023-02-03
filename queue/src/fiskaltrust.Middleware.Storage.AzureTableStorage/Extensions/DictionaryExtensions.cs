using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, TValue> ConvertToCaseInSensitive<TValue>(this Dictionary<string, TValue> dictionary)
        {
            var resultDictionary = new Dictionary<string, TValue>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var kvp in dictionary)
            {
                resultDictionary.Add(kvp.Key, kvp.Value);
            }
            return resultDictionary;
        }
    }
}
