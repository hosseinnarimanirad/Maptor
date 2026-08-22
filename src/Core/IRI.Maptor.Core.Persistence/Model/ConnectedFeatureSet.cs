//using System;
//using System.Collections.Generic;

//using IRI.Maptor.Core.Common.Primitives;
//using IRI.Maptor.Core.Spatial.Primitives;
//using IRI.Maptor.Core.Persistence.DataSources;


//namespace IRI.Maptor.Core.Persistence.Model;

////SqlFeatureSet which is connected to its source. For Add, Remove, Update actions
////This class was used first for FFSDB editing tool
//public class ConnectedFeatureSet<TGeometryAware> : IFeatureSet<TGeometryAware, Point> where TGeometryAware : class, IGeometryAware<Point>
//{
//    public VectorDataSource DataSource { get; set; }
//    public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//    public int Srid { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//    public List<Field> Fields { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//    public List<TGeometryAware> Features { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

//    public ConnectedFeatureSet(List<TGeometryAware> features, VectorDataSource dataSource, string title)
//    {
//        Features = features;

//        DataSource = dataSource;

//        Title = title;
//    }
//}
