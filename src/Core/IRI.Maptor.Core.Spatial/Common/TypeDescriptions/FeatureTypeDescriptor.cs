//using IRI.Maptor.Core.Common.Abstractions;
//using IRI.Maptor.Core.Spatial.Primitives;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;

//namespace IRI.Maptor.Core.Spatial.Common.TypeDescriptions;


//public class FeatureTypeDescriptor<T> : CustomTypeDescriptor where T : IPoint, new()
//{
//    private readonly Feature<T> _feature;

//    public FeatureTypeDescriptor(ICustomTypeDescriptor parent, object instance)
//        : base(parent)
//    {
//        _feature = (Feature<T>)instance;
//    }

//    public override PropertyDescriptorCollection GetProperties()
//    {
//        return GetProperties(null);
//    }

//    public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
//    {
//        var props = new List<PropertyDescriptor>();

//        if (_feature?.Attributes != null)
//        {
//            // Add each dictionary key as a property
//            if (_feature.Attributes != null)
//            {
//                foreach (var key in _feature.Attributes.Keys)
//                {
//                    props.Add(new DictionaryEntryPropertyDescriptor<T>(key));
//                }
//            }
//        }

//        // We deliberately do NOT include Id, TheGeometry, LabelAttribute
//        return new PropertyDescriptorCollection(props.ToArray());
//    }
//}
