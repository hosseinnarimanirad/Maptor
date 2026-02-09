using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Sta.Persistence.Abstractions;

namespace IRI.Maptor.Ket.SqlServerPersistence;

public class SqlServerDataSource : VectorDataSource, IEditableVectorDataSource
{
    const string _outputSpatialAttribute = "_shape";

    protected BoundingBox _webMercatorExtent = BoundingBox.NaN;

    protected string _connectionString;

    protected string? _tableName;

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
            if (_webMercatorExtent.IsNaN() && _spatialColumnName != null)
            {
                this._webMercatorExtent = GetBoundingBox();
            }

            return _webMercatorExtent;
        }
        protected set
        {
            _webMercatorExtent = value;
        }
    }

    public override int Srid { get => GetSrid(); /*protected set => _ = value;*/ }

    protected SqlServerDataSource() : base(new List<Field>())
    {

    }

    private SqlServerDataSource(string connectionString, string? spatialColumnName = null, string? labelColumnName = null) : base(new List<Field>())
    {
        this._connectionString = connectionString;

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

    public SqlServerDataSource(string connectionString, string tableName, string? spatialColumnName = null, string? labelColumnName = null)
        : this(connectionString, spatialColumnName, labelColumnName)
    {
        this._tableName = tableName;

    }

    public static SqlServerDataSource CreateForQueryString(string connectionString, string queryString, string spatialColumnName, string? labelColumnName = null)
    {
        SqlServerDataSource result = new SqlServerDataSource(connectionString, spatialColumnName, labelColumnName)
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




    //

    protected string MakeWhereClause(string? whereClause)
    {
        return string.IsNullOrWhiteSpace(whereClause) ? string.Empty : FormattableString.Invariant($" WHERE ({whereClause}) ");
    }

    #region CRUD

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
}