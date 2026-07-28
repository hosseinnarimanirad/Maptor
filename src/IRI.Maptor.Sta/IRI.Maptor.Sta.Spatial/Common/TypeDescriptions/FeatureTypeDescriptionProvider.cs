//using IRI.Maptor.Sta.Common.Abstractions;
//using IRI.Maptor.Sta.Spatial.Primitives;
//using System;
//using System.ComponentModel;
 
//namespace IRI.Maptor.Sta.Spatial.Common.TypeDescriptions;


//public class FeatureTypeDescriptionProvider<T> : TypeDescriptionProvider where T : IPoint, new()
//{
//    private readonly TypeDescriptionProvider _baseProvider;

//    public FeatureTypeDescriptionProvider()
//        : this(TypeDescriptor.GetProvider(typeof(Feature<T>))) { }

//    public FeatureTypeDescriptionProvider(TypeDescriptionProvider baseProvider)
//        : base(baseProvider)
//    {
//        _baseProvider = baseProvider;
//    }

//    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
//    {
//        return new FeatureTypeDescriptor<T>(_baseProvider.GetTypeDescriptor(objectType, instance), instance);
//    }
//}
