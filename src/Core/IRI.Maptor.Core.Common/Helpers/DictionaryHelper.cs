using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Helpers;

public class DictionaryHelper
{
    /// <summary>
    /// Returns attributes that differ between old and new dictionaries.
    /// Keys only in one dict are included (OldValue or NewValue may be null).
    /// </summary>
    public static IEnumerable<(string Name, object? OldValue, object? NewValue)> GetChangedAttributes(
        Dictionary<string, object>? oldDict,
        Dictionary<string, object>? newDict)
    {
        var allKeys = (oldDict?.Keys ?? Enumerable.Empty<string>())
            .Union(newDict?.Keys ?? Enumerable.Empty<string>())
            .Distinct();
        foreach (var key in allKeys)
        {
            object? oldVal = null;
            object? newVal = null;
            oldDict?.TryGetValue(key, out oldVal);
            newDict?.TryGetValue(key, out newVal);
            if (!Equals(oldVal, newVal))
                yield return (key, oldVal, newVal);
        }
    }
    public static bool AreAttributesEqual(Dictionary<string, object>? a, Dictionary<string, object>? b)
    {
        if (a == b) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var bVal)) return false;
            if (!Equals(kv.Value, bVal)) return false;
        }
        return true;
    }

    public static Dictionary<string, object> Copy(Dictionary<string, object> dic)
    {
        var result = new System.Collections.Generic.Dictionary<string, object>();

        if (dic == null)
            return result;
         
        foreach (var kv in dic)
            result[kv.Key] = kv.Value;

        return result;
    }
}
