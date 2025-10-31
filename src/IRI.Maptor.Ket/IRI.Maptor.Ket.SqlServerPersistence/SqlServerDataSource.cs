using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Ket.SqlServerPersistence;

public class SqlServerDataSource : VectorDataSource/*<Feature<Point>>*/, IEditableVectorDataSource/*<Feature<Point>, Point>*/
{
    const string _outputSpatialAttribute = "_shape";

    protected BoundingBox _extent = BoundingBox.NaN;

    protected string _connectionString;

    protected string _tableName;

    protected string? _queryString;

    protected string? _spatialColumnName;

    protected string? _labelColumnName;

    public Action<Feature<Point>>? AddAction;

    public Action<int>? RemoveAction;

    public Action<Feature<Point>>? UpdateAction;

    public string? IdColumnName { get; set; }

    public override BoundingBox WebMercatorExtent
    {
        get
        {
            if (double.IsNaN(_extent.Width) || double.IsNaN(_extent.Height) && _spatialColumnName != null)
            {
                this._extent = GetBoundingBox();
            }

            return _extent;
        }
        protected set
        {
            _extent = value;
        }
    }

    public override int Srid { get => GetSrid(); protected set => _ = value; }

    protected SqlServerDataSource() : base(new List<Field>())
    {

    }

    public SqlServerDataSource(string connectionString, string tableName, string? spatialColumnName = null, string? labelColumnName = null) : base(new List<Field>())
    {
        this._connectionString = connectionString;

        this._tableName = tableName;

        this._spatialColumnName = spatialColumnName;

        this._labelColumnName = labelColumnName;

        if (spatialColumnName == null)
        {
            this.WebMercatorExtent = BoundingBox.NaN;
        }
        else
        {
            //IMPORTANT!
            //this.Extent = GetGeometries().GetBoundingBox();
        }

    }

    public static SqlServerDataSource CreateForQueryString(string connectionString, string queryString, string spatialColumnName, string labelColumnName = null)
    {
        SqlServerDataSource result = new SqlServerDataSource(connectionString, null, spatialColumnName, labelColumnName)
        {
            _queryString = queryString,
        };

        return result;
    }


    private string GetTable()
    {
        return this._tableName ?? $" ({this._queryString}) A";
    }

    protected static string GetWhereClause(string spatialColumnName, BoundingBox boundingBox, int srid)
    {
        return FormattableString.Invariant($" {spatialColumnName}.STIntersects(GEOMETRY::STPolyFromText('{boundingBox.AsWkt()}',{srid})) = 1 ");
    }

    //protected string GetCommandString(string wktGeometryFilter, bool returnOnlyGeometry = true)
    //{
    //    if (string.IsNullOrWhiteSpace(wktGeometryFilter))
    //    {
    //        return GetCommandString(null, returnOnlyGeometry);
    //    }
    //    else
    //    {
    //        var wkbArray = IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.Parse(wktGeometryFilter).STAsBinary().Value;

    //        return MakeSelectCommandWithWkb(wkbArray, returnOnlyGeometry);
    //    }
    //}

    //protected string MakeSelectCommand(string whereClause)
    //{
    //    return FormattableString.Invariant($"SELECT {_spatialColumnName} FROM {GetTable()} {MakeWhereClause(whereClause)}");
    //}

    protected string MakeSelectCommand(string? whereClause, bool returnOnlyGeometry)
    {
        if (returnOnlyGeometry)
        {
            return FormattableString.Invariant($"SELECT {_spatialColumnName} FROM {GetTable()} {MakeWhereClause(whereClause)}");
        }
        else
        {
            return FormattableString.Invariant($"SELECT * FROM {GetTable()} {MakeWhereClause(whereClause)}");
        }
    }

    protected string MakeSelectCommandWithWkt(string wktGeometryFilter, bool returnOnlyGeometry)
    {
        if (string.IsNullOrWhiteSpace(wktGeometryFilter))
        {
            return MakeSelectCommand(string.Empty, returnOnlyGeometry);
        }

        var srid = GetSrid();

        if (returnOnlyGeometry)
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromText({wktGeometryFilter},{srid});
                SELECT {_spatialColumnName} FROM {GetTable()} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
        else
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromText({wktGeometryFilter},{srid});
                SELECT * FROM {GetTable()} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
    }

    protected string MakeSelectCommandWithWkb(byte[] wkbGeometryFilter, bool returnOnlyGeometry)
    {
        if (wkbGeometryFilter == null)
        {
            return MakeSelectCommand(string.Empty, returnOnlyGeometry);
        }

        var srid = GetSrid();

        var wkbString = IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ToHexStringUsingBitFiddle(wkbGeometryFilter, true);

        if (returnOnlyGeometry)
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromWKB({wkbString},{srid});
                SELECT {_spatialColumnName} FROM {GetTable()} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
        else
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromWKB({wkbString},{srid});
                SELECT * FROM {GetTable()} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
    }

    public int GetSrid()
    {
        SqlConnection connection = null;

        int srid;

        try
        {
            connection = new SqlConnection(_connectionString);

            SqlCommand command = new SqlCommand(FormattableString.Invariant($"SELECT TOP 1 {_spatialColumnName}.STSrid FROM {GetTable()} WHERE NOT {_spatialColumnName} IS NULL AND {_spatialColumnName}.STIsValid()=1"), connection);

            connection.Open();

            List<Geometry<Point>> geometries = new List<Geometry<Point>>();

            srid = (int)command.ExecuteScalar();

            connection.Close();
        }
        catch
        {
            srid = 0;
        }
        finally
        {
            connection.Close();
        }

        return srid;
    }

    public BoundingBox GetBoundingBox()
    {
        //var query = string.Format(CultureInfo.InvariantCulture, "SELECT {0}.STEnvelope() FROM {1} ", _spatialColumnName, GetTable());
        var query = FormattableString.Invariant($"SELECT {_spatialColumnName}.STEnvelope() FROM {GetTable()} ");

        var envelopes = SelectGeometries(query);

        //return IRI.Maptor.Ket.SqlServerSpatialExtension.Helpers.SqlSpatialHelper.GetBoundingBoxFromEnvelopes(envelopes);
        return envelopes.GetBoundingBox();
    }


    protected List<T> Select<T>(string selectQuery, string connectionString = null)
    {
        if (connectionString == null)
        {
            connectionString = _connectionString;
        }

        SqlConnection connection = new SqlConnection(connectionString);

        var command = new SqlCommand(selectQuery, connection);

        connection.Open();

        List<T> result = new List<T>();

        SqlDataReader reader = command.ExecuteReader();

        if (!reader.HasRows)
        {
            return new List<T>();
        }

        while (reader.Read())
        {
            result.Add((T)reader[0]);//2565 ms
        }

        connection.Close();

        return result;
    }

    public List<T> Select<T>(string selectQuery, Func<IDataRecord, T> mapFunction)
    {
        SqlConnection connection = new SqlConnection(_connectionString);

        var command = new SqlCommand(selectQuery, connection);

        connection.Open();

        List<T> result = new List<T>();

        SqlDataReader reader = command.ExecuteReader();

        if (!reader.HasRows)
        {
            return new List<T>();
        }

        while (reader.Read())
        {
            result.Add(mapFunction(reader));
        }

        connection.Close();

        return result;
    }

    public List<Dictionary<string, object>> SelectFeatures(string selectQuery, bool returnWkt = false)
    {
        return SqlServerInfrastructure.SelectFeatures(_connectionString, selectQuery, returnWkt);
    }

    public List<Dictionary<string, object>> GetFeaturesWhereIntersects(string wktGeometryFilter, bool returnGeometryAsWktForm = false)
    {
        return SelectFeatures(MakeSelectCommandWithWkt(wktGeometryFilter, false), returnGeometryAsWktForm);
    }

    #region Get Geometries

    ////3857: web mercator; 102100: web mercator
    //public override List<Geometry<Point>> GetGeometries(BoundingBox boundingBox)
    //{
    //    var srid = GetSrid();

    //    var whereClause = GetWhereClause(_spatialColumnName, boundingBox, srid);

    //    return this.GetGeometries(whereClause);
    //}

    //public override List<Geometry<Point>> GetGeometries(Geometry<Point> geometry)
    //{
    //    if (geometry == null)
    //    {
    //        return GetGeometries();
    //    }

    //    return GetGeometriesWhereIntersects(geometry.AsWkt());
    //}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="whereClause">Do not include the "WHERE", e.g. coulumn01 = someValue</param>
    /// <returns></returns>
    public List<Geometry<Point>> GetGeometries(string whereClause)
    {
        return SelectGeometries(MakeSelectCommand(whereClause, true));
    }

    protected List<Geometry<Point>> SelectGeometries(string selectQuery, string? connectionString = null)
    {
        if (connectionString == null)
            connectionString = _connectionString;

        SqlConnection connection = new SqlConnection(connectionString);

        connection.Open();

        var command = new SqlCommand(selectQuery, connection);

        List<Geometry<Point>> geometries = new List<Geometry<Point>>();

        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                return new List<Geometry<Point>>();
            }

            while (reader.Read())
            {
                //approach 1
                //geometries.Add(SqlGeometry.STGeomFromWKB(new System.Data.SqlTypes.SqlBytes((byte[])reader[0]), srid).MakeValid()); //4100-4200 ms
                //approach 2
                //geometries.Add(SqlGeometry.Deserialize(reader.GetSqlBytes(0))); //3220 ms

                //approach 3 

                //geometries.Add(reader[0] as SqlGeometry);//2565 ms

                geometries.Add((reader[0] as SqlGeometry).AsGeometry());//2565 ms

            }
        }

        connection.Close();

        return geometries;
    }


    public List<Geometry<Point>> GetGeometriesWhereIntersects(string wktGeometryFilter)
    {
        return SelectGeometries(MakeSelectCommandWithWkt(wktGeometryFilter, true));
    }

    public List<Geometry<Point>> GetGeometriesWhereIntersects(byte[] wkbGeometryFilter)
    {
        //if (wkbGeometryFilter == null)
        //{
        //    return GetGeometries();
        //}

        return SelectGeometries(MakeSelectCommandWithWkb(wkbGeometryFilter, true));
    }

    #endregion


    //#region Get Geometry Label Pair

    //public List<NamedGeometry> GetGeometryLabelPairs(string whereClause)
    //{
    //    SqlConnection connection = new SqlConnection(_connectionString);

    //    //SqlCommand command =
    //    //    new SqlCommand(
    //    //        string.Format(CultureInfo.InvariantCulture, "SELECT {0}, {1} FROM {2} {3} ", _spatialColumnName, _labelColumnName, GetTable(), whereClause ?? string.Empty),
    //    //        connection);

    //    var commandText = string.IsNullOrWhiteSpace(_labelColumnName) ?
    //        MakeSelectCommand(whereClause, true) :
    //        FormattableString.Invariant($"SELECT {_spatialColumnName}, {_labelColumnName} FROM {GetTable()} {MakeWhereClause(whereClause)} ");

    //    SqlCommand command = new SqlCommand(commandText, connection);

    //    connection.Open();

    //    var result = new List<NamedGeometry>();

    //    using (SqlDataReader reader = command.ExecuteReader())
    //    {

    //        if (!reader.HasRows)
    //        {
    //            return new List<NamedGeometry>();
    //        }

    //        if (string.IsNullOrWhiteSpace(_labelColumnName))
    //        {
    //            while (reader.Read())
    //            {
    //                result.Add(new NamedGeometry(((SqlGeometry)reader[0]).AsGeometry(), string.Empty));
    //            }
    //        }
    //        else
    //        {
    //            while (reader.Read())
    //            {
    //                result.Add(new NamedGeometry(((SqlGeometry)reader[0]).AsGeometry(), reader[1]?.ToString()));
    //            }
    //        }
    //    }

    //    connection.Close();

    //    return result;
    //}

    //protected List<NamedGeometry> SelectGeometryLabelPairs(string selectQuery, string connectionString = null)
    //{
    //    if (connectionString == null)
    //    {
    //        connectionString = _connectionString;
    //    }

    //    SqlConnection connection = new SqlConnection(connectionString);

    //    connection.Open();

    //    var command = new SqlCommand(selectQuery, connection);

    //    List<NamedGeometry> geometries = new List<NamedGeometry>();

    //    using (var reader = command.ExecuteReader())
    //    {
    //        if (!reader.HasRows)
    //        {
    //            return new List<NamedGeometry>();
    //        }

    //        while (reader.Read())
    //        {
    //            //approach 1
    //            //geometries.Add(SqlGeometry.STGeomFromWKB(new System.Data.SqlTypes.SqlBytes((byte[])reader[0]), srid).MakeValid()); //4100-4200 ms
    //            //approach 2
    //            //geometries.Add(SqlGeometry.Deserialize(reader.GetSqlBytes(0))); //3220 ms

    //            //approach 3 
    //            geometries.Add(new NamedGeometry(((SqlGeometry)reader[0]).AsGeometry(), string.Empty));//2565 ms

    //        }
    //    }

    //    connection.Close();

    //    return geometries;
    //}

    //#endregion


    //#region Get Entire Feature

    //public DataTable ExecuteSql(string commandString)
    //{
    //    SqlConnection connection = new SqlConnection(_connectionString);

    //    SqlCommand command = new SqlCommand(commandString, connection);

    //    connection.Open();

    //    DataTable result = new DataTable();

    //    SqlDataAdapter adapter = new SqlDataAdapter(command);

    //    adapter.Fill(result);

    //    //result.Load(command.ExecuteReader());

    //    connection.Close();

    //    return result;
    //}

    //public DataTable GetEntireFeatures()
    //{
    //    return GetEntireFeatures(string.Empty);
    //}

    //public DataTable GetEntireFeatures(string whereClause)
    //{
    //    //return ExecuteSql(string.Format(CultureInfo.InvariantCulture, "SELECT * FROM {0} {1} ", GetTable(), whereClause ?? string.Empty));
    //    return ExecuteSql(FormattableString.Invariant($"SELECT * FROM {GetTable()} {MakeWhereClause(whereClause)} "));
    //}

    //#endregion


    #region GetAsFeatureSet

    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        if (geometry is not null)
        {
            var selectQuery = MakeSelectCommandWithWkb(geometry.AsWkb(), false);

            return GetAsFeatureSet(selectQuery);
        }
        else
        {
            return GetAsFeatureSet(MakeSelectCommand(null, false));
        }
    }

    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox)
    {
        var whereClause = GetWhereClause(_spatialColumnName, boundingBox, GetSrid());

        return GetAsFeatureSet(MakeSelectCommand(whereClause, false));
    }

    public FeatureSet<Point> GetAsFeatureSetWhereIntersectsWkt(string wktGeometryFilter)
    {
        //return QueryFeatures(GetCommandString(wktGeometryFilter, false));
        return GetAsFeatureSet(MakeSelectCommandWithWkt(wktGeometryFilter, false));
    }

    private FeatureSet<Point> GetAsFeatureSet(string selectQuery)
    {
        SqlConnection connection = new SqlConnection(_connectionString);

        FeatureSet<Point> result = FeatureSet<Point>.Create(string.Empty, new List<Feature<Point>>());

        try
        {
            var command = new SqlCommand(selectQuery, connection);

            connection.Open();

            SqlDataReader reader = command.ExecuteReader();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var type = reader.GetFieldType(i);

                if (type != typeof(SqlGeometry))
                {
                    result.Fields.Add(new Field() { Name = reader.GetName(i), Type = type.ToString() });
                }
            }

            if (!reader.HasRows)
            {
                return result;
            }

            while (reader.Read())
            {
                var dict = new Dictionary<string, object>();

                var feature = new Feature<Point>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);

                    while (dict.Keys.Contains(fieldName))
                    {
                        fieldName = $"{fieldName}_";
                    }

                    if (reader.IsDBNull(i))
                    {
                        dict.Add(fieldName, null);
                    }
                    else
                    {
                        if (reader[i] is SqlGeometry)
                        {
                            feature.TheGeometry = ((SqlGeometry)reader[i]).AsGeometry();
                        }
                        else
                        {
                            dict.Add(fieldName, reader[i]);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(IdColumnName))
                {
                    feature.Id = (int)dict[IdColumnName];
                }

                feature.Attributes = dict;

                result.Features.Add(feature);
            }

            connection.Close();
        }
        catch (Exception ex)
        {
            connection.Close();
        }

        return result;

    }

    #endregion

    //public override List<Feature<Point>> GetFeatures(Geometry<Point> geometry)
    //{
    //    //var selectQuery = GetCommandString(geometry?.AsWkt(), false);
    //    var selectQuery = MakeSelectCommandWithWkb(geometry?.AsWkb(), false);

    //    var featureSet = GetAsFeatureSet(selectQuery);

    //    return featureSet?.Features;
    //}


    //public override void Update(TGeometryAware<Point> newFeature)
    //{
    //    this.UpdateAction?.Invoke(newFeature);
    //}

    //public override void UpdateFeature(IGeometryAware<Point> feature)
    //{
    //    throw new NotImplementedException();
    //}

    //public override void Remove(IGeometryAware<Point> feature)
    //{
    //    Remove(feature.Id);
    //}


    //public FeatureSet GetSqlFeatures()
    //{
    //    return QueryFeatures();
    //}

    //public FeatureSet GetSqlFeatures(Geometry<Point> geometry)
    //{
    //    var boundingBox = geometry.GetBoundingBox();

    //    var featureSet = GetAsFeatureSet(boundingBox);
    //    //.Features
    //    //.Where(s => s.TheSqlGeometry?.STIntersects(geometry).IsTrue == true)
    //    //.ToList();

    //    featureSet.Features = featureSet.Features.Where(s => s.TheGeometry?.Intersects(geometry) == true).ToList();

    //    return featureSet;
    //}




    //

    protected string MakeWhereClause(string? whereClause)
    {
        return string.IsNullOrWhiteSpace(whereClause) ? string.Empty : FormattableString.Invariant($" WHERE ({whereClause}) ");
    }




    //protected override Feature<Point> ToFeatureMappingFunc(Feature<Point> geometryAware)
    //{
    //    return geometryAware;
    //}

    //public override List<Feature<Point>> GetGeometryAwares(Geometry<Point>? geometry)
    //{
    //    return GetAsFeatureSet(geometry).Features;
    //}

    #region CRUD


    //public void Add(IGeometryAware<Point> newValue)
    //{
    //    Add(newValue as Feature<Point>);
    //}

    //public void Remove(IGeometryAware<Point> value)
    //{
    //    Remove(value as Feature<Point>);
    //}

    //public void Update(IGeometryAware<Point> newValue)
    //{
    //    Update(newValue as Feature<Point>);
    //}

    public void Add(Feature<Point> newValue)
    {
        this.AddAction?.Invoke(newValue);
    }

    public void Remove(int featureId)
    {
        this.RemoveAction?.Invoke(featureId);
    }

    public void Remove(Feature<Point> value)
    {
        throw new NotImplementedException();
    }

    public void Update(Feature<Point> newValue)
    {
        throw new NotImplementedException();
    }

    public void SaveChanges()
    {
        throw new NotImplementedException();
    }


    #endregion

    public void ExecuteSql(string command)
    {
        SqlServerInfrastructure.ExecuteNonQuery(command, _connectionString);
    }

    public override FeatureSet<Point> Search(string searchText)
    {
        throw new NotImplementedException();
    }

    //public override List<Feature<Point>> GetGeometryAwares(BoundingBox boundingBox)
    //{
    //    throw new NotImplementedException();
    //}


    //public override FeatureSet<Point> GetAsFeatureSet(string searchText)
    //{
    //    throw new NotImplementedException();
    //}
}