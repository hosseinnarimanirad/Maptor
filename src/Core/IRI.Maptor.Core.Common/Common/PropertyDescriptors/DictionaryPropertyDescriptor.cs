using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace IRI.Maptor.Core.Common.PropertyDescriptors;

public class DictionaryPropertyDescriptor : PropertyDescriptor
{
    System.Collections.IDictionary _dictionary;
    object _key;

    public DictionaryPropertyDescriptor(System.Collections.IDictionary d, object key)
        : base(key.ToString(), null)
    {
        _dictionary = d;
        _key = key;
    }

    public override Type PropertyType
    {
        get { return _dictionary[_key]?.GetType() ?? typeof(string); }
    }

    public override void SetValue(object component, object value)
    {
        _dictionary[_key] = value;
    }

    public override object GetValue(object component)
    {
        return _dictionary[_key];
    }

    public override bool IsReadOnly
    {
        get { return false; }
    }

    public override Type ComponentType
    {
        get { return null; }
    }

    public override bool CanResetValue(object component)
    {
        return false;
    }

    public override void ResetValue(object component)
    {
    }

    public override bool ShouldSerializeValue(object component)
    {
        return false;
    }
}
