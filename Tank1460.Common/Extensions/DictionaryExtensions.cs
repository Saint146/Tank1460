using System.Collections.Generic;
using System.Linq;

namespace Tank1460.Common.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> ShallowClone<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        return dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}