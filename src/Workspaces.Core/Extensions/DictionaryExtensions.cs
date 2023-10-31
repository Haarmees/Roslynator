using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Roslynator
{
    internal static class DictionaryExtensions
    {
        internal static Dictionary<TKey, IEnumerable<TValue>> Merge<TKey, TValue>(
            this Dictionary<TKey, IEnumerable<TValue>> sourceDictionary,
            Dictionary<TKey, IEnumerable<TValue>> mergeDictionary)
        {
            foreach (var kvp in mergeDictionary)
            {
                if (sourceDictionary.ContainsKey(kvp.Key))
                {
                    sourceDictionary[kvp.Key] = sourceDictionary[kvp.Key].Concat(kvp.Value);
                }
                else
                {
                    sourceDictionary.Add(kvp.Key, kvp.Value);
                }
            }

            return sourceDictionary;
        }
    }
}