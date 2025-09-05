using System.Threading.Tasks;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Sta.Persistence.DataSources;

public abstract class VectorDataSource : IVectorDataSource
{
    public virtual BoundingBox WebMercatorExtent { get; protected set; }

    public abstract int Srid { get; protected set; }

    public virtual GeometryType? GeometryType { get; protected set; }

    public List<Field> Fields { get; set; } = new List<Field>();

    public VectorDataSource(List<Field> fields)
    {
        this.Fields = fields;
    }

    #region Get as FeatureSet

    public virtual FeatureSet<Point> GetAsFeatureSet() => GetAsFeatureSet(Geometry<Point>.Empty);

    public abstract FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox);

    public abstract FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry);

    public virtual FeatureSet<Point> GetAsFeatureSet(double mapScale, BoundingBox boundingBox) => GetAsFeatureSet(boundingBox);


    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync() => Task.Run(GetAsFeatureSet);

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox) => Task.Run(() => GetAsFeatureSet(boundingBox));

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry) => Task.Run(() => GetAsFeatureSet(geometry));

    public virtual Task<FeatureSet<Point>> GetAsFeatureSetAsync(double mapScale, BoundingBox boundingBox) => Task.Run(() => GetAsFeatureSet(mapScale, boundingBox));

    #endregion



    public abstract FeatureSet<Point> Search(string searchText);

}

//public static class ToDataTableDefaultMappings
//{
//    public static Func<List<T>, DataTable> GenericTypeMapping<T>()
//    {
//        return list =>
//        {
//            var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

//            var dataTable = new DataTable();

//            if (list.Count == 0)
//                return dataTable;

//            foreach (var property in properties)
//            {
//                var type = (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ?
//                                Nullable.GetUnderlyingType(property.PropertyType) : property.PropertyType);

//                dataTable.Columns.Add(property.Name, type);
//            }

//            for (int row = 0; row < list.Count; row++)
//            {
//                var dataRow = dataTable.NewRow();

//                for (int i = 0; i < properties.Length; i++)
//                {
//                    var value = properties[i].GetValue(list[row]);

//                    if (value == null)
//                        continue;

//                    dataRow[i] = value;
//                }

//                dataTable.Rows.Add(dataRow);
//            }

//            return dataTable;
//        };
//    }

//    public static DataTable SqlFeatureTypeMapping(List<Feature> list)
//    {
//        var dataTable = new DataTable();

//        if (list.Count == 0)
//            return dataTable;

//        var columnNames = list.First().Attributes.Select(dict => dict.Key.ToString()).Distinct().ToList();

//        dataTable.Columns.AddRange(columnNames.Select(c => new DataColumn(c)).ToArray());

//        for (int row = 0; row < list.Count; row++)
//        {
//            var dataRow = dataTable.NewRow();

//            for (int col = 0; col < columnNames.Count; col++)
//            {
//                var value = list[row].Attributes[columnNames[col]];

//                if (value == null)
//                    continue;

//                dataRow[col] = value;
//            }

//            dataTable.Rows.Add(dataRow);
//        }

//        return dataTable;
//    }
//}