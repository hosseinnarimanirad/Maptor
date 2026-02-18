using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Helpers;

public class DictionaryHelper
{
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
