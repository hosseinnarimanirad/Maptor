using System.Data.OleDb;
using System.Threading.Tasks;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.PersonalGdb;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Persistence.DataSources;
using IRI.Maptor.Ket.PersonalGdbPersistence.Model;
using IRI.Maptor.Sta.SpatialReferenceSystem;

namespace IRI.Maptor.Ket.PersonalGdbPersistence;

public class PersoanlGdbDataSource : VectorDataSource
{
    protected BoundingBox _webMercatorExtent = BoundingBox.NaN;

    private const string _defaultSpatialColumnName = "SHAPE";

    public override BoundingBox WebMercatorExtent
    {
        get
        {
            if (_webMercatorExtent.IsNaN() && _spatialColumnName != null)
            {
                this.SetBoundingBoxAndSrid();
            }

            return _webMercatorExtent;
        }
        protected set
        {
            _webMercatorExtent = value;
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

    private int _srid;
    public override int Srid { get => _srid; /*protected set;*/ }


    public override string SourceAddress => _mdbFileName;

    public string SearchColumn { get; set; }

    public PersoanlGdbDataSource(
        string mdbFileName,
        string tableName,
        string tableDisplayName,
        string? spatialColumnName = null,
        string? labelColumnName = null,
        Func<Point, Point>? onTheFlyProj = null,
        List<GdbCodedValueDomain>? domains = null,
        List<GdbItemColumnInfo>? columns = null) : base(new List<Field>())
    {
        this._mdbFileName = mdbFileName;

        this._tableName = tableName;

        this._tableDisplayName = tableDisplayName;

        this._spatialColumnName = spatialColumnName ?? _defaultSpatialColumnName;

        this._labelColumnName = labelColumnName;

        _onTheFlyProj = onTheFlyProj ?? (p => p);

        _domains = domains;

        _columns = columns;

        SetBoundingBoxAndSrid();
    }


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

        var wkbString = IRI.Maptor.Sta.Common.Helpers.HexStringHelper.ToHexStringUsingBitFiddle(wkbGeometryFilter, true);

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

                    this._srid = srsDictionary[srid]?.Srid ?? 0;
                    this.GeometryType = ((EsriPGDBColumnShapeType)(int)dataReader["ShapeType"]).AsGeometryType();
                    this._webMercatorExtent = new BoundingBox(minX, minY, maxX, maxY).Transform(_onTheFlyProj);
                    //return new BoundingBox(minX, minY, maxX, maxY);
                }
            }
        }

        //return BoundingBox.NaN;
    }

    protected FeatureSet<Point> Select(Geometry<Point> geometryBoundingBox)
    {
        return Select(geometryBoundingBox, null);
    }

    private FeatureSet<Point> Select(Geometry<Point>? geometryBoundingBox, string? searchText)
    {
        var fields = new List<Field>();
        var featuresList = new List<Feature<Point>>();

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
                            fields.Add(new Field() { Name = dataReader.GetName(i), TypeFullName = type?.FullName ?? "object" });
                        }
                    }

                    if (!dataReader.HasRows)
                    {
                        var emptyResult = FeatureSet<Point>.Create(string.Empty, new List<Feature<Point>>());
                        emptyResult.Fields = fields;
                        return emptyResult;
                    }

                    if (fields.All(f => f.Name != _labelColumnName))
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

                            featuresList.Add(feature);

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

        var result = FeatureSet<Point>.Create(string.Empty, featuresList);
        result.Fields = fields;
        return result;
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(Geometry<Point>? geometry)
    {
        return Task.Run(() => Select(geometry));
    }

    public override Task<FeatureSet<Point>> GetAsFeatureSetAsync(BoundingBox boundingBox)
    {
        throw new NotImplementedException();
    }

    public override Task<FeatureSet<Point>> SearchAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(SearchColumn))
        {
            return Task.FromResult<FeatureSet<Point>>(null);
        }

        return Task.FromResult(Select(null, searchText));
    }
}
