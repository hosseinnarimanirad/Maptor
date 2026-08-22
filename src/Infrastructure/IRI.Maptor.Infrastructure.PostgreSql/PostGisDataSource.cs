using Npgsql;
using System.Globalization;
using System.Data;
using System.Threading.Tasks;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.DataSources;

namespace IRI.Maptor.Infrastructure.PostgreSql;

// todo: implement methods
public class PostGisDataSource : VectorDataSource/*<Feature<Point>>*/
{
    public override BoundingBox WebMercatorExtent { get; protected set; }
    public override int Srid { get => throw new NotImplementedException(); /*protected set => throw new NotImplementedException();*/ }

    private string _connectionString;

    private string _tableName;

    private string _spatialColumnName;

    private string _schema;

    private string _serverName;

    public override string SourceAddress => $"Postgres Server: {_serverName}";

    public PostGisDataSource(
        string server,
        string user,
        string password,
        string database,
        string port,
        string tableName,
        string spatialColumnName = null,
        string schema = "public")
        : this(PostgreSqlInfrastructure.GetConnectionString(server, user, password, database, port), tableName, spatialColumnName, schema)
    {
        _serverName = server;
    }

    private PostGisDataSource(string connectionString, string tableName, string spatialColumnName, string schema = "public") : base(new List<Field>())
    {
        this._connectionString = connectionString;
        //string.Format("Server={0}; UID={1}; PWD={2}; Database={3}; Port={4}", server, user, password, database, port);

        this._tableName = tableName;

        this._spatialColumnName = spatialColumnName;

        this._schema = schema;

        this.WebMercatorExtent = GetGeometries().GetBoundingBox();
    }

    private string GetProperSelectForSpatialColumn(string columnName)
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "encode(ST_AsBinary({0}),'hex')", _spatialColumnName);
    }

    public DataTable ExecuteSql(string commandString)
    {
        NpgsqlConnection connection = new NpgsqlConnection(_connectionString);

        NpgsqlCommand command = new NpgsqlCommand(commandString, connection);

        connection.Open();

        System.Data.DataTable result = new System.Data.DataTable();

        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command);

        adapter.Fill(result);

        connection.Close();

        return result;
    }

    public DataTable GetEntireFeaturesWhereIntersects(List<Point> region)
    {
        string wktRegion = string.Format(System.Globalization.CultureInfo.InvariantCulture, " POLYGON(({0})) ",
            string.Join(",", region.Select(i => string.Format(" {0} {1}", i.X, i.Y))));

        return GetEntireFeaturesWhereIntersects(wktRegion);
    }

    private DataTable GetEntireFeaturesWhereIntersects(string wktRegion)
    {
        throw new NotImplementedException();
    }

    public DataTable GetAttributeColumns(string whereClause)
    {
        List<string> columns = PostgreSqlInfrastructure.GetColumnNames(_connectionString, _schema ?? "public", _tableName);

        if (!columns.Contains(_spatialColumnName))
            throw new NotImplementedException();

        string command =
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "SELECT {0} FROM {1} {2}",
                string.Join(",", columns.Where(i => i != _spatialColumnName)),
                _tableName,
                string.IsNullOrEmpty(whereClause) ? string.Empty : whereClause);

        return ExecuteSql(command);
    }

    public DataTable GetAttributeColumnsWhereIntersects(string wktRegion)
    {
        string whereClause = string.Format(CultureInfo.InvariantCulture, " WHERE ST_INTERSECTS({0},'{1}'::geometry)", _spatialColumnName, wktRegion);

        return GetAttributeColumns(whereClause);
    }

    public DataTable GetAttributeColumnsWhereIntersects(List<Point> region)
    {
        string wktRegion = string.Format(System.Globalization.CultureInfo.InvariantCulture, " POLYGON(({0})) ",
            string.Join(",", region.Select(i => string.Format(" {0} {1}", i.X, i.Y))));

        return GetAttributeColumnsWhereIntersects(wktRegion);
    }

    public List<Geometry<Point>> GetGeometriesWhereIntersects(string wktRegion)
    {
        string whereClause = string.Format(CultureInfo.InvariantCulture, " WHERE ST_INTERSECTS({0},'{1}'::geometry)", _spatialColumnName, wktRegion);

        return GetGeometries(whereClause);
    }

    public List<Geometry<Point>> GetGeometriesWhereIntersects(List<Point> simpleRegion)
    {
        string wktRegion = string.Format(" POLYGON(({0})) ",
            string.Join(",", simpleRegion.Select(i => string.Format(System.Globalization.CultureInfo.InvariantCulture, " {0} {1}", i.X, i.Y))));

        return GetGeometriesWhereIntersects(wktRegion);
    }

    #region Override Methods

    public List<Geometry<Point>> GetGeometries()
    {
        return GetGeometries(string.Empty);
    }

    //public override List<NamedGeometry<Point>> GetGeometryLabelPairsForDisplay(BoundingBox boundingBox)
    //{
    //    throw new NotImplementedException();
    //}

    public List<Geometry<Point>> GetGeometries(string whereClause)
    {
        NpgsqlConnection connection = new NpgsqlConnection(_connectionString);

        connection.Open();

        string commandString =
            string.Format(CultureInfo.InvariantCulture, "SELECT {0} FROM {1} {2}",
                GetProperSelectForSpatialColumn(_spatialColumnName),
                _tableName,
                whereClause);

        NpgsqlCommand command = new NpgsqlCommand(commandString, connection);

        List<Geometry<Point>> geometries = new List<Geometry<Point>>();

        var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var obj = IRI.Maptor.Core.Common.Helpers.HexStringHelper.ToByteArray(reader[0].ToString());

            //geometries.Add(SqlGeometry.STGeomFromWKB(new System.Data.SqlTypes.SqlBytes(obj), 0));
            geometries.Add((Geometry<Point>)Geometry<Point>.FromWkb(obj, 0));
        }

        connection.Close();

        return geometries;
    }

    public List<Geometry<Point>> GetGeometries(BoundingBox boundingBox)
    {
        Geometry<Point> boundary = boundingBox.AsGeometry<Point>(this.Srid);

        return GetGeometries(boundary);
    }

    public List<Geometry<Point>> GetGeometries(Geometry<Point> geometry)
    {
        return GetGeometries().Where(i => i.Intersects(geometry)).ToList();
    }


    //public DataTable GetEntireFeaturesWhereIntersects(string wktRegion)
    //{
    //    string whereClause = string.Format(CultureInfo.InvariantCulture, " WHERE ST_INTERSECTS({0},'{1}'::geometry)", _spatialColumnName, wktRegion);

    //    return GetEntireFeatures(whereClause);
    //}

    //public override DataTable GetEntireFeatures(BoundingBox geometry)
    //{
    //    var boundary = geometry.AsGeometry(GetSrid());

    //    return GetEntireFeatures(boundary);
    //}

    //public override DataTable GetEntireFeatures()
    //{
    //    string where = string.Empty;

    //    return GetEntireFeatures(where);
    //}

    //public override DataTable GetEntireFeatures(Geometry<Point> geometry)
    //{
    //    string wkt = new string(geometry.AsWkt());//.STAsText().Value);

    //    return GetEntireFeaturesWhereIntersects(wkt);
    //}

    ////WHERE ST_INTERSECTS(A,B)
    //public override DataTable GetEntireFeatures(string whereClause)
    //{
    //    List<string> columns = Infrastructure.PostgreSqlInfrastructure.GetColumnNames(_connectionString, _schema, _tableName);

    //    if (!columns.Contains(_spatialColumnName))
    //        throw new NotImplementedException();

    //    string command =
    //        string.Format(System.Globalization.CultureInfo.InvariantCulture, "SELECT {0} FROM {1} {2}",
    //            string.Join(",", columns.Select(i => i == _spatialColumnName ? GetProperSelectForSpatialColumn(i) : i)),
    //            _tableName,
    //            string.IsNullOrEmpty(whereClause) ? string.Empty : whereClause);

    //    return ExecuteSql(command);
    //}

    //public override void SaveChanges()
    //{
    //    throw new NotImplementedException();
    //}


    #endregion




    //
    protected string MakeWhereClause(string whereClause)
    {
        return string.IsNullOrWhiteSpace(whereClause) ? string.Empty : FormattableString.Invariant($" WHERE ({whereClause}) ");
    }


    public virtual Task<List<Geometry<Point>>> GetGeometriesAsync(string whereClause)
    {
        return Task.Run(() => { return GetGeometries(whereClause); });
    }

    //protected override Feature<Point> ToFeatureMappingFunc(Feature<Point> geometryAware)
    //{
    //    throw new NotImplementedException();
    //}

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        throw new NotImplementedException();
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        throw new NotImplementedException();
    }

    //public override List<Feature<Point>> GetGeometryAwares(Geometry<Point>? geometry)
    //{
    //    throw new NotImplementedException();
    //}

    //public override void Add(Feature<Point> newValue)
    //{
    //    throw new NotImplementedException();
    //}

    //public override void Remove(Feature<Point> value)
    //{
    //    throw new NotImplementedException();
    //}

    //public override void Update(Feature<Point> newValue)
    //{
    //    throw new NotImplementedException();
    //}

    public override Task<FeatureSet<Point>> SearchAsync(string searchText)
    {
        throw new NotImplementedException();
    }

    //public override List<Feature<Point>> GetGeometryAwares(BoundingBox boundingBox)
    //{
    //    throw new NotImplementedException();
    //}
}
