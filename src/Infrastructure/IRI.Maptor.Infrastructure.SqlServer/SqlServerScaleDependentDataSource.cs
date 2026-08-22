//using System.Data;

//using Microsoft.SqlServer.Types;

//using IRI.Maptor.Extensions;
//using IRI.Maptor.Core.Spatial.Primitives;
//using IRI.Maptor.Core.Spatial.Analysis;
//using IRI.Maptor.Core.Common.Primitives;
//using IRI.Maptor.Core.Spatial.Helpers; 

//namespace IRI.Maptor.Infrastructure.SqlServer;

//public class SqlServerScaleDependentDataSource : SqlServerDataSource, IScaleDependentDataSource
//{
//    SqlServerSourceParameter _pyramidParameters;

//    List<int> _levels;

//    string _schema;

//    const string _pyramidTableSpatialColumn = "GEO";

//    const string _defaultSchema = "GIS";

//    protected SqlServerScaleDependentDataSource()
//    {

//    }

//    public SqlServerScaleDependentDataSource(SqlServerSourceParameter sourceParameters, SqlServerSourceParameter pyramidParameters, bool createPyramid = false, string schema = _defaultSchema)
//    {
//        this._connectionString = sourceParameters.ConnectionString;

//        this._tableName = sourceParameters.TableName;

//        this._spatialColumnName = sourceParameters.SpatialColumnName;

//        this._labelColumnName = sourceParameters.LabelColumn;

//        this._queryString = sourceParameters.QueryString;

//        this._pyramidParameters = pyramidParameters;

//        this._schema = string.IsNullOrEmpty(schema) ? "GIS" : schema;

//        if (sourceParameters.SpatialColumnName == null)
//        {
//            this.WebMercatorExtent = BoundingBox.NaN;
//        }
//        else
//        {
//            //IMPORTANT!
//            //this.Extent = GetGeometries().GetBoundingBox();
//        }

//        if (createPyramid)
//        {
//            Initialize();
//        }

//        _levels = GetLevels();

//    }

//    private void Initialize()
//    {
//        var command = $"CREATE TABLE GIS.{_pyramidParameters.TableName} ({_pyramidTableSpatialColumn} GEOMETRY NOT NULL, MBB GEOMETRY NOT NULL, Level INT NOT NULL)";

//        if (SqlServerInfrastructure.Exists(_pyramidParameters.TableName, _pyramidParameters.ConnectionString))
//            SqlServerInfrastructure.Delete(_pyramidParameters.TableName, _pyramidParameters.ConnectionString);

//        SqlServerInfrastructure.ExecuteNonQuery(command, this._pyramidParameters.ConnectionString);

//        //var originalGeometries = GetGeometries();

//        //for (int i = 1; i < 11; i++)
//        //{
//        //    var geometries = originalGeometries.Simplify(SqlServerSpatialExtension.Analysis.SimplificationType.AdditiveByAreaAngle, i);

//        //    geometries = geometries.Simplify(SqlServerSpatialExtension.Analysis.SimplificationType.AdditiveByArea, i);

//        //    CopyToSqlServer(_pyramidParameters.TableName, geometries, i);
//        //}
//        var originalGeometries = GetAsFeatureSet().Features.Select(f => f.TheGeometry).ToList();// GetGeometries();

//        for (int i = 15; i > 1; i--)
//        {
//            System.Diagnostics.Debug.WriteLine(string.Empty);

//            SimplificationParameters param = new SimplificationParameters { AngleThreshold = 0.98 };

//            var threshold = WebMercatorUtility.CalculateGroundResolution(i, 0);

//            param.DistanceThreshold = threshold;
//            param.AreaThreshold = threshold * threshold;

//            var geometries = originalGeometries.Simplify(SimplificationType.CumulativeAreaAngle, new SimplificationParameters() { AngleThreshold = 0.98 }, true);

//            geometries = geometries.Simplify(SimplificationType.CumulativeTriangleRoutine, new SimplificationParameters() { AngleThreshold = 0.98 }, true);

//            CopyToSqlServer(_pyramidParameters.TableName, geometries, i - 1);
//        }
//    }

//    private void CopyToSqlServer(string tableName, List<Geometry<Point>> geometries, int level)
//    {
//        var infra = new SqlServerInfrastructure();

//        DataTable table = new DataTable();

//        table.Columns.Add(GetSpatialColumn("Geo"));

//        table.Columns.Add(GetSpatialColumn("MBB"));

//        table.Columns.Add(new DataColumn("Level", typeof(int)) { AllowDBNull = false });

//        for (int i = 0; i < geometries.Count; i++)
//        {
//            var row = table.NewRow();

//            row["Level"] = level;

//            row["Geo"] = geometries[i];

//            row["MBB"] = geometries[i].GetBoundingBox().AsGeometry<Point>(GetSrid());//.STEnvelope();

//            table.Rows.Add(row);
//        }

//        infra.InsertTable(this._pyramidParameters.ConnectionString, table, "dbo", tableName, false, false);
//    }

//    private DataColumn GetSpatialColumn(string name)
//    {
//        DataColumn spatialColumn = new DataColumn(name, typeof(SqlGeometry));

//        spatialColumn.AllowDBNull = false;

//        return spatialColumn;
//    }



//    //public List<Geometry<Point>> GetGeometries(double mapScale)
//    //{
//    //    return GetGeometries(mapScale, string.Empty);
//    //}

//    public List<Geometry<Point>> GetGeometries(double mapScale, BoundingBox boundingBox)
//    {
//        int srid = GetSrid();

//        var whereClause = GetWhereClause(_pyramidTableSpatialColumn, boundingBox, srid);

//        return GetGeometries(mapScale, whereClause);
//    }

//    public List<Geometry<Point>> GetGeometries(double mapScale, string whereClause)
//    {
//        int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale, 35);

//        if (zoomLevel > _levels.Max())
//        {
//            zoomLevel = _levels.Max();
//        }

//        if (string.IsNullOrWhiteSpace(whereClause))
//        {
//            whereClause = $" Level = {zoomLevel} ";
//        }
//        else
//        {
//            whereClause = $" (Level = {zoomLevel} ) AND {whereClause} ";
//        }

//        return SelectGeometries(FormattableString.Invariant($"SELECT {_pyramidTableSpatialColumn} FROM {_pyramidParameters.TableName} {MakeWhereClause(whereClause)} "), _pyramidParameters.ConnectionString);
//    }



//    //public List<NamedGeometry> GetGeometryLabelPairs(double mapScale, BoundingBox boundingBox)
//    //{
//    //    int srid = GetSrid();

//    //    //var whereClause = $" (GEO.STIntersects(GEOMETRY::STPolyFromText('{boundingBox.AsWkt()}',{srid})) = 1) ";
//    //    var whereClause = GetWhereClause(_pyramidTableSpatialColumn, boundingBox, srid);

//    //    int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale, 35);

//    //    if (!(_levels?.Count < 1) && zoomLevel > _levels.Max())
//    //    {
//    //        //zoomLevel = _levels.Max();

//    //        return GetNamedGeometries(boundingBox);// GetGeometryLabelPairsForDisplay(boundingBox);
//    //    }
//    //    else
//    //    {
//    //        return GetGeometryLabelPairs(mapScale, whereClause);
//    //    }
//    //}

//    //public List<NamedGeometry> GetGeometryLabelPairs(double mapScale, string whereClause)
//    //{
//    //    int zoomLevel = WebMercatorUtility.GetZoomLevel(mapScale, 35);

//    //    if (zoomLevel > _levels.Max())
//    //    {
//    //        //zoomLevel = _levels.Max();

//    //        return GetGeometryLabelPairs(whereClause);
//    //    }

//    //    if (string.IsNullOrWhiteSpace(whereClause))
//    //    {
//    //        whereClause = $" (Level = {zoomLevel}) ";
//    //    }
//    //    else
//    //    {
//    //        whereClause = $" (Level = {zoomLevel} ) AND {whereClause} ";
//    //    }

//    //    //return SelectGeometries(
//    //    //    string.Format(
//    //    //        CultureInfo.InvariantCulture,
//    //    //        "SELECT GEO FROM {0} {1} ", _pyramidParameters.TableName, whereClause ?? string.Empty),
//    //    //    _pyramidParameters.ConnectionString);

//    //    //return SelectGeometries(FormattableString.Invariant($"SELECT GEO FROM {_pyramidParameters.TableName} {whereClause ?? string.Empty} "), _pyramidParameters.ConnectionString);

//    //    //return GetGeometryLabelPairs(whereClause);
//    //    //throw new Exception("label column not available in _P table");
//    //    return SelectGeometryLabelPairs(FormattableString.Invariant($"SELECT {_pyramidTableSpatialColumn} FROM {_pyramidParameters.TableName} {MakeWhereClause(whereClause)} "), _pyramidParameters.ConnectionString);
//    //}



//    //public Task<List<Geometry<Point>>> GetGeometriesAsync(double scale)
//    //{
//    //    return Task.Run(() => { return GetGeometries(scale); });
//    //}

//    //public Task<List<Geometry<Point>>> GetGeometriesAsync(double mapScale, BoundingBox boundingBox)
//    //{
//    //    return Task.Run(() => { return GetGeometries(mapScale, boundingBox); });
//    //}

//    public List<int> GetLevels()
//    {
//        return Select<int>(FormattableString.Invariant($"SELECT DISTINCT(Level) FROM {_pyramidParameters.TableName} "), _pyramidParameters.ConnectionString);
//    }
//}