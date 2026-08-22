//using IRI.Maptor.Core.Common.Abstractions;
//using IRI.Maptor.Core.Spatial.Primitives;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Reflection;

//namespace IRI.Maptor.Core.Spatial.Common.TypeDescriptions;

//public class DictionaryEntryPropertyDescriptor<T> : PropertyDescriptor where T : IPoint, new()
//{
//    private readonly string _key;

//    public DictionaryEntryPropertyDescriptor(string key)
//        : base(key, null)
//    {
//        _key = key;
//    }

//    public override bool CanResetValue(object component) => false;
//    public override Type ComponentType => typeof(Feature<T>);
//    public override object GetValue(object component)
//    {
//        var feature = (Feature<T>)component;
//        return feature.Attributes != null && feature.Attributes.TryGetValue(_key, out var value)
//            ? value
//            : null;
//    }
//    public override bool IsReadOnly => false;
//    public override Type PropertyType => typeof(object);

//    public override void ResetValue(object component) { }

//    public override void SetValue(object component, object value)
//    {
//        var feature = (Feature<T>)component;
//        feature.Attributes[_key] = value;

//        //if (component is INotifyPropertyChanged inpc)
//        //    inpc.PropertyChanged?.Invoke(component, new PropertyChangedEventArgs(_key));
//    }

//    public override bool ShouldSerializeValue(object component) => true;
//}


