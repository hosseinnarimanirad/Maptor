using System.Data.OleDb;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.PersonalGdb;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.PersonalGdbPersistence;

public class PersoanlGdbDataSource : VectorDataSource//<Feature<Point>>// RelationalDbSource<Feature<Point>>
{
    protected BoundingBox _extent = BoundingBox.NaN;

    private const string _defaultSpatialColumnName = "SHAPE";

    public override BoundingBox WebMercatorExtent
    {
        get
        {
            if (double.IsNaN(_extent.Width) || double.IsNaN(_extent.Height) && _spatialColumnName != null)
            {
                //this._extent = GetGeometries().GetBoundingBox();
                //this._extent = GetBoundingBoxAndSrid();
                this.SetBoundingBoxAndSrid();
            }

            return _extent;
        }
        protected set
        {
            _extent = value;
        }
    }

    protected string _mdbFileName;

    protected string _tableName;

    protected string _tableDisplayName;

    protected string _spatialColumnName;

    protected string? _labelColumnName;

    private Func<Point, Point> _onTheFlyProj;

    private readonly List<GdbCodedValueDomain>? _domains;

    private readonly List<GdbItemColumnInfo>? _columns;

    public Action<Feature<Point>>? AddAction;

    public Action<int>? RemoveAction;

    public Action<Feature<Point>>? UpdateAction;

    public string? IdColumnName { get; set; }
    public override int Srid { get; protected set; }

    public string SearchColumn { get; set; }

    public PersoanlGdbDataSource(
        string mdbFileName,
        string tableName,
        string tableDisplayName,
        string? spatialColumnName = null,
        string? labelColumnName = null,
        Func<Point, Point> onTheFlyProj = null,
        List<GdbCodedValueDomain>? domains = null,
        List<GdbItemColumnInfo>? columns = null)
    {
        this._mdbFileName = mdbFileName;

        this._tableName = tableName;

        this._tableDisplayName = tableDisplayName;

        this._spatialColumnName = spatialColumnName ?? _defaultSpatialColumnName;

        this._labelColumnName = labelColumnName;

        _onTheFlyProj = onTheFlyProj;

        _domains = domains;

        _columns = columns;

        SetBoundingBoxAndSrid();
    }

    //


    protected static string GetWhereClause(string spatialColumnName, BoundingBox boundingBox, int srid)
    {
        return FormattableString.Invariant($" {spatialColumnName}.STIntersects(GEOMETRY::STPolyFromText('{boundingBox.AsWkt()}',{srid})) = 1 ");
    }

    protected string MakeWhereClause(string whereClause)
    {
        return string.IsNullOrWhiteSpace(whereClause) ? string.Empty : FormattableString.Invariant($" WHERE ({whereClause}) ");
    }


    protected string MakeSelectCommand(string? whereClause, bool returnOnlyGeometry)
    {
        if (returnOnlyGeometry)
        {
            return FormattableString.Invariant($"SELECT {_spatialColumnName} FROM {_tableName} {MakeWhereClause(whereClause)}");
        }
        else
        {
            return FormattableString.Invariant($"SELECT * FROM {_tableName} {MakeWhereClause(whereClause)}");
        }
    }

    protected string MakeSelectCommandWithWkt(string wktGeometryFilter, bool returnOnlyGeometry)
    {
        if (string.IsNullOrWhiteSpace(wktGeometryFilter))
        {
            return MakeSelectCommand(string.Empty, returnOnlyGeometry);
        }

        if (returnOnlyGeometry)
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromText({wktGeometryFilter},{Srid});
                SELECT {_spatialColumnName} FROM {_tableName} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
        else
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromText({wktGeometryFilter},{Srid});
                SELECT * FROM {_tableName} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
    }

    protected string MakeSelectCommandWithWkb(byte[] wkbGeometryFilter, bool returnOnlyGeometry)
    {
        if (wkbGeometryFilter == null)
        {
            return MakeSelectCommand(string.Empty, returnOnlyGeometry);
        }

        var wkbString = IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ByteToHexBitFiddle(wkbGeometryFilter, true);

        if (returnOnlyGeometry)
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromWKB({wkbString},{Srid});
                SELECT {_spatialColumnName} FROM {_tableName} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
        else
        {
            return FormattableString.Invariant($@"
                DECLARE @filter GEOMETRY;
                SET @filter = GEOMETRY::STGeomFromWKB({wkbString},{Srid});
                SELECT * FROM {_tableName} WHERE {_spatialColumnName}.STIntersects(@filter)=1");
        }
    }



    //


    public void SetBoundingBoxAndSrid()
    {
        using (var conn = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(_mdbFileName)))
        {
            conn.Open();

            var srsDictionary = PersonalGdbInfrastructure.GetSpatialReferenceSystems(conn);

            var query = FormattableString.Invariant(
                        @$"SELECT        TableName, ShapeType, FieldName, ExtentLeft, ExtentBottom, ExtentRight, ExtentTop, SRID
                            FROM            GDB_GeomColumns
                            WHERE        (TableName = '{_tableName}')");

            var cmd = new OleDbCommand(query, conn);

            using (var dataReader = cmd.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    var minX = (double)dataReader["ExtentLeft"];
                    var maxX = (double)dataReader["ExtentRight"];
                    var minY = (double)dataReader["ExtentBottom"];
                    var maxY = (double)dataReader["ExtentTop"];
                    var srid = (int)dataReader["SRID"];

                    this.Srid = srsDictionary[srid]?.Srid ?? 0;
                    this.GeometryType = ((EsriPGDBColumnShapeType)(int)dataReader["ShapeType"]).AsGeometryType();
                    this._extent = new BoundingBox(minX, minY, maxX, maxY).Transform(_onTheFlyProj);
                    //return new BoundingBox(minX, minY, maxX, maxY);
                }
            }
        }

        //return BoundingBox.NaN;
    }

    //private string GetConnectionString()
    //{
    //    if (System.IO.File.Exists(_mdbFileName))
    //    {
    //        return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_mdbFileName};Persist Security Info=False;";
    //    }
    //    else
    //    {
    //        return string.Empty;
    //    }
    //}

    //protected List<Geometry<Point>> SelectGeometries(string selectQuery)
    //{
    //    List<Geometry<Point>> geometries = new List<Geometry<Point>>();

    //    using (var conn = new OleDbConnection(GetConnectionString()))
    //    {
    //        conn.Open();

    //        var query = $"SELECT {_spatialColumnName} FROM {_tableName}";

    //        var cmd = new OleDbCommand(query, conn);

    //        using (var dataReader = cmd.ExecuteReader())
    //        {
    //            while (dataReader.Read())
    //            {
    //                var esriByteGeometry = (byte[])dataReader[0];

    //                var esriShape = IRI.Maptor.Sta.PersonalGdb.EsriPGdbHelper.ParseToEsriShape(esriByteGeometry, 0);

    //                geometries.Add(esriShape.AsGeometry());
    //            }
    //        }
    //    }

    //    return geometries;
    //}

    protected FeatureSet<Point> Select(Geometry<Point> geometryBoundingBox)
    {
        return Select(geometryBoundingBox, null);
    }

    private FeatureSet<Point> Select(Geometry<Point>? geometryBoundingBox, string? searchText)
    {
        FeatureSet<Point> result = FeatureSet<Point>.Create(string.Empty, new List<Feature<Point>>());

        using (var conn = new OleDbConnection(PersonalGdbInfrastructure.GetConnectionString(_mdbFileName)))
        {
            var commandString = string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(SearchColumn) ?
                FormattableString.Invariant($"SELECT * FROM {_tableName}") :
                FormattableString.Invariant($"SELECT * FROM {_tableName} WHERE {SearchColumn} LIKE '%{searchText}%'");

            using (var command = new OleDbCommand(commandString, conn))
            {
                conn.Open();

                using (var dataReader = command.ExecuteReader())
                {
                    int geoIndex = -1;

                    var schema = dataReader.GetColumnSchemaAsync();

                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        var type = dataReader.GetFieldType(i);

                        //if (type != typeof(SqlGeometry))
                        if (dataReader.GetName(i).ToUpper() == "SHAPE" && type == typeof(byte[]))
                        {
                            geoIndex = i;
                        }
                        else
                        {
                            result.Fields.Add(new Field() { Name = dataReader.GetName(i), Type = type.ToString() });
                        }
                    }

                    if (!dataReader.HasRows)
                        return result;

                    if (result.Fields.All(f => f.Name != _labelColumnName))
                        _labelColumnName = null;

                    while (dataReader.Read())
                    {
                        try
                        {
                            var dict = new Dictionary<string, object?>();

                            var feature = new Feature<Point>();

                            var esriByteGeometry = (byte[])dataReader[geoIndex];

                            var geometry = EsriPGdbHelper.ParseToEsriShape(esriByteGeometry, 0).AsGeometry().Transform(_onTheFlyProj, SridHelper.WebMercator);

                            feature.TheGeometry = geometry;

                            //var schemaT = dataReader.GetSchemaTable();

                            for (int i = 0; i < dataReader.FieldCount; i++)
                            {
                                var fieldName = dataReader.GetName(i);

                                var columnInfo = _columns?.FirstOrDefault(c => c.Name == fieldName);

                                var domain = _domains?.FirstOrDefault(d => d.DomainName == columnInfo?.DomainName);

                                while (dict.Keys.Contains(fieldName))
                                {
                                    fieldName = $"{fieldName}_";
                                }

                                if (dataReader.IsDBNull(i))
                                {
                                    dict.Add(fieldName, null);
                                }
                                else
                                {
                                    if (i != geoIndex)
                                    {
                                        var value = dataReader[i];

                                        var mappedDomain = domain?.Values.FirstOrDefault(d => d.Code.ToString() == value?.ToString())?.Name;

                                        dict.Add(fieldName, mappedDomain ?? value);
                                    }
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(IdColumnName))
                            {
                                feature.Id = (int)dict[IdColumnName];
                            }

                            feature.Attributes = dict;

                            feature.LabelAttribute = _labelColumnName;

                            if (!geometryBoundingBox.IsNullOrEmpty() && !geometry.Intersects(geometryBoundingBox))
                                continue;

                            result.Features.Add(feature);

                        }
                        catch (Exception ex)
                        {
                            //throw new NotImplementedException("PersoanlGdbDataSource > Select");
                        }
                    }
                }
            }

            conn.Close();
        }

        return result;
    }

    //protected override Feature<Point> ToFeatureMappingFunc(Feature<Point> geometryAware)
    //{
    //    return geometryAware;
    //}


    public override FeatureSet<Point> GetAsFeatureSet(Geometry<Point>? geometry)
    {
        if (geometry is not null)
        {
            //var selectQuery = MakeSelectCommandWithWkb(geometry.AsWkb(), false);

            return Select(geometry);
        }
        else
        {
            return Select(geometry/*MakeSelectCommand(null, false)*/);
        }
    }

    //public override List<Feature<Point>> GetGeometryAwares(Geometry<Point>? geometry)
    //{
    //    return GetAsFeatureSet(geometry).Features;
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

    //public override void SaveChanges()
    //{
    //    throw new NotImplementedException();
    //}

    public override FeatureSet<Point> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(SearchColumn))
        {
            return null;
        }

        return Select(null, searchText);
    }

    public override FeatureSet<Point> GetAsFeatureSet(BoundingBox boundingBox)
    {
        throw new NotImplementedException();
    } 
}
