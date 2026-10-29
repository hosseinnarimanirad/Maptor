using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Npgsql;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Infrastructure.EfCore.Extensions;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;

namespace IRI.Maptor.Tests.PostgreSql;

/// <summary>Creates a throwaway PostGIS database for <see cref="PostGisIntegrationTest"/> and drops it afterwards.</summary>
public sealed class PostGisDatabaseFixture : IAsyncLifetime
{
    public string DatabaseName { get; } = "maptor_tests_" + Guid.NewGuid().ToString("N")[..12];

    public string ConnectionString { get; private set; } = "";

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (PostGisTestDatabase.UnavailableReason is not null)
            return;

        ConnectionString = await PostGisTestDatabase.CreateDatabaseAsync(DatabaseName);

        var builder = new NpgsqlDataSourceBuilder(ConnectionString);
        builder.UseMaptor();
        DataSource = builder.Build();
    }

    public async Task DisposeAsync()
    {
        if (PostGisTestDatabase.UnavailableReason is not null)
            return;

        await DataSource.DisposeAsync();
        await PostGisTestDatabase.DropDatabaseAsync(DatabaseName);
    }
}

/// <summary>
/// Round trips against a real PostGIS server, through both the Npgsql type plugin and the EF Core plugin.
/// Skipped when no server is reachable (see <see cref="PostGisTestDatabase"/>).
/// </summary>
public class PostGisIntegrationTest : IClassFixture<PostGisDatabaseFixture>
{
    private readonly PostGisDatabaseFixture _fixture;

    public PostGisIntegrationTest(PostGisDatabaseFixture fixture) => _fixture = fixture;

    private static Geometry<Point> Square(double x, double y, int srid = 4326)
        => Geometry<Point>.FromWkt(FormattableString.Invariant($"POLYGON (({x} {y}, {x + 0.1} {y}, {x + 0.1} {y + 0.1}, {x} {y + 0.1}, {x} {y}))"), srid);

    #region ADO.NET (Npgsql type plugin)

    [PostGisFact]
    public async Task Read_AllShapes_KeepSrid_DropZM()
    {
        var cases = new (string Ewkt, string ExpectedWkt, int Srid)[]
        {
            ("SRID=4326;POINT(51.4 35.7)", "POINT (51.4 35.7)", 4326),
            ("POINT(1 2)", "POINT (1 2)", 0),
            ("SRID=32639;POINT Z(500000 3950000 1200.5)", "POINT (500000 3950000)", 32639),
            ("SRID=4326;LINESTRING M(0 0 10,1 1 20)", "LINESTRING (0 0, 1 1)", 4326),
            ("SRID=4326;POLYGON((0 0,20 0,20 20,0 20,0 0),(5 5,5 15,15 15,15 5,5 5))", "POLYGON ((0 0, 20 0, 20 20, 0 20, 0 0), (5 5, 5 15, 15 15, 15 5, 5 5))", 4326),
            ("SRID=3857;MULTIPOLYGON ZM(((0 0 1 2,10 0 1 2,10 10 1 2,0 0 1 2)),((20 20 1 2,30 20 1 2,30 30 1 2,20 20 1 2)))", "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 0)), ((20 20, 30 20, 30 30, 20 20)))", 3857),
            ("SRID=4326;MULTIPOINT((10 20),(30 40))", "MULTIPOINT ((10 20), (30 40))", 4326),
        };

        foreach (var (ewkt, expectedWkt, srid) in cases)
        {
            await using var command = _fixture.DataSource.CreateCommand($"SELECT '{ewkt}'::geometry");
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            var geometry = await reader.GetFieldValueAsync<Geometry<Point>>(0);

            Assert.Equal(expectedWkt, geometry.AsWkt());
            Assert.Equal(srid, geometry.Srid);
        }
    }

    [PostGisFact]
    public async Task Read_Geography_ThroughGetValue()
    {
        await using var command = _fixture.DataSource.CreateCommand("SELECT 'SRID=4326;POINT(51.4 35.7)'::geography");

        var value = await command.ExecuteScalarAsync();

        var geometry = Assert.IsType<Geometry<Point>>(value);
        Assert.Equal("POINT (51.4 35.7)", geometry.AsWkt());
        Assert.Equal(4326, geometry.Srid);
    }

    [PostGisFact]
    public async Task Write_Parameters_GeometryGeographyAndNull()
    {
        var square = Square(51, 35);

        await using var command = _fixture.DataSource.CreateCommand(
            "SELECT ST_AsEWKT(@untyped), pg_typeof(@untyped)::text, ST_AsEWKT(@geography), pg_typeof(@geography)::text, @nothing IS NULL");
        command.Parameters.AddWithValue("untyped", square);
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "geography", Value = Geometry<Point>.Create(10, 20, 0), DataTypeName = "geography" });
        command.Parameters.Add(new NpgsqlParameter { ParameterName = "nothing", Value = DBNull.Value, DataTypeName = "geometry" });

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        Assert.Equal("SRID=4326;POLYGON((51 35,51.1 35,51.1 35.1,51 35.1,51 35))", reader.GetString(0));
        Assert.Equal("geometry", reader.GetString(1));
        Assert.Equal("SRID=4326;POINT(10 20)", reader.GetString(2));   // geography assumes 4326 for SRID 0
        Assert.Equal("geography", reader.GetString(3));
        Assert.True(reader.GetBoolean(4));
    }

    [PostGisFact]
    public async Task Arrays_RoundTrip()
    {
        var points = new[] { Geometry<Point>.Create(1, 2, 4326), Geometry<Point>.Create(3, 4, 4326) };

        await using var command = _fixture.DataSource.CreateCommand("SELECT @points");
        command.Parameters.AddWithValue("points", points);

        var result = Assert.IsType<Geometry<Point>[]>(await command.ExecuteScalarAsync());

        Assert.Equal(points.Select(p => p.AsWkt()), result.Select(p => p.AsWkt()));
        Assert.All(result, p => Assert.Equal(4326, p.Srid));
    }

    [PostGisFact]
    public async Task LargeValue_RoundTrips_SyncAndAsync()
    {
        // ~3.2 MB of EWKB, far beyond Npgsql's 8 KB buffer
        var points = Enumerable.Range(0, 200_000).Select(i => new Point(i * 0.001, Math.Sin(i * 0.001))).ToList();
        var line = Geometry<Point>.Create(points, GeometryType.LineString, 4326);

        await using var command = _fixture.DataSource.CreateCommand("SELECT ST_NPoints(@line), @line");
        command.Parameters.AddWithValue("line", line);

        await using (var reader = await command.ExecuteReaderAsync())
        {
            await reader.ReadAsync();
            Assert.Equal(200_000, reader.GetInt32(0));
            Assert.Equal(line.AsEwkb(), (await reader.GetFieldValueAsync<Geometry<Point>>(1)).AsEwkb());
        }

        using (var reader = command.ExecuteReader())
        {
            reader.Read();
            Assert.Equal(line.AsEwkb(), reader.GetFieldValue<Geometry<Point>>(1).AsEwkb());
        }
    }

    [PostGisFact]
    public async Task BinaryCopy_Imports()
    {
        await using var connection = await _fixture.DataSource.OpenConnectionAsync();
        await using (var create = new NpgsqlCommand("CREATE TABLE copy_target (g geometry, gg geography)", connection))
            await create.ExecuteNonQueryAsync();

        await using (var importer = await connection.BeginBinaryImportAsync("COPY copy_target (g, gg) FROM STDIN (FORMAT BINARY)"))
        {
            await importer.StartRowAsync();
            await importer.WriteAsync(Square(51, 35), "geometry");
            await importer.WriteAsync(Square(51, 35), "geography");
            await importer.CompleteAsync();
        }

        await using var check = new NpgsqlCommand("SELECT ST_AsEWKT(g), ST_AsEWKT(gg) FROM copy_target", connection);
        await using var reader = await check.ExecuteReaderAsync();
        await reader.ReadAsync();

        Assert.Equal("SRID=4326;POLYGON((51 35,51.1 35,51.1 35.1,51 35.1,51 35))", reader.GetString(0));
        Assert.Equal(reader.GetString(0), reader.GetString(1));
    }

    #endregion

    #region EF Core

    public class Parcel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Geometry<Point> Shape { get; set; } = null!;
        public Geometry<Point>? Footprint { get; set; }
        public Geometry<Point>? Local { get; set; }
    }

    private sealed class ParcelContext(string connectionString) : DbContext
    {
        public static readonly Geometry<Point> SeedShape = Geometry<Point>.FromWkt("POINT (51.4 35.7)", 4326);

        public DbSet<Parcel> Parcels => Set<Parcel>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(connectionString, x => x.UseMaptor());

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
            => configurationBuilder.ConfigureMaptorGeometry("GEOGRAPHY");    // as Barg/Gas configure it

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var parcel = modelBuilder.Entity<Parcel>();
            parcel.ToTable("parcels");
            parcel.Property(p => p.Footprint).HasColumnType("geometry(Polygon,4326)");
            parcel.Property(p => p.Local!).IsGeometry();
            parcel.HasData(new Parcel { Id = 1, Name = "seed", Shape = SeedShape });
        }
    }

    [PostGisFact]
    public async Task EfCore_CreateWriteReadTrackAndQuery()
    {
        await using (var db = new ParcelContext(_fixture.ConnectionString))
        {
            var script = db.Database.GenerateCreateScript();
            Assert.Contains("CREATE EXTENSION IF NOT EXISTS postgis", script);
            Assert.Contains("\"Shape\" GEOGRAPHY NOT NULL", script);
            Assert.Contains("\"Footprint\" geometry(Polygon,4326)", script);

            await db.Database.EnsureCreatedAsync();

            db.Parcels.Add(new Parcel
            {
                Name = "a",
                Shape = Square(52, 36),
                Footprint = Square(52, 36),
                Local = Geometry<Point>.Create(500000, 3950000, 32639),
            });
            db.Parcels.Add(new Parcel { Name = "nulls", Shape = Geometry<Point>.Create(51.5, 35.5, 4326) });
            await db.SaveChangesAsync();
        }

        await using (var db = new ParcelContext(_fixture.ConnectionString))
        {
            var seed = await db.Parcels.AsNoTracking().SingleAsync(p => p.Id == 1);
            Assert.Equal(ParcelContext.SeedShape.AsEwkb(), seed.Shape.AsEwkb());

            var a = await db.Parcels.AsNoTracking().SingleAsync(p => p.Name == "a");
            Assert.Equal(Square(52, 36).AsEwkb(), a.Shape.AsEwkb());
            Assert.Equal(Square(52, 36).AsEwkb(), a.Footprint!.AsEwkb());
            Assert.Equal(32639, a.Local!.Srid);

            var nulls = await db.Parcels.AsNoTracking().SingleAsync(p => p.Name == "nulls");
            Assert.Null(nulls.Footprint);
            Assert.Null(nulls.Local);

            // change tracking: replaced value, in-place mutation, untouched entity
            var replaced = await db.Parcels.SingleAsync(p => p.Name == "a");
            replaced.Local = Geometry<Point>.Create(600000, 3950000, 32639);
            var mutated = await db.Parcels.SingleAsync(p => p.Name == "nulls");
            mutated.Shape.Points![0].X = 51.6;
            var untouched = await db.Parcels.SingleAsync(p => p.Id == 1);
            db.ChangeTracker.DetectChanges();

            Assert.Equal(EntityState.Modified, db.Entry(replaced).State);
            Assert.Equal(EntityState.Modified, db.Entry(mutated).State);
            Assert.Equal(EntityState.Unchanged, db.Entry(untouched).State);
            await db.SaveChangesAsync();
        }

        await using (var db = new ParcelContext(_fixture.ConnectionString))
        {
            Assert.Equal("POINT (600000 3950000)", (await db.Parcels.AsNoTracking().SingleAsync(p => p.Name == "a")).Local!.AsWkt());
            Assert.Equal("POINT (51.6 35.5)", (await db.Parcels.AsNoTracking().SingleAsync(p => p.Name == "nulls")).Shape.AsWkt());

            var probe = Geometry<Point>.Create(52.05, 36.05, 4326);
            var hits = await db.Parcels
                .FromSql($"SELECT * FROM parcels WHERE ST_Intersects(\"Shape\", {probe}::geography)")
                .AsNoTracking().Select(p => p.Name).ToListAsync();
            Assert.Equal(new[] { "a" }, hits);

            var centroid = await db.Database
                .SqlQuery<Geometry<Point>>($"SELECT ST_Centroid(\"Footprint\") AS \"Value\" FROM parcels WHERE \"Name\" = 'a'")
                .SingleAsync();
            Assert.Equal(GeometryType.Point, centroid.Type);
            Assert.Equal(4326, centroid.Srid);

            var local = Geometry<Point>.Create(600000, 3950000, 32639);
            Assert.Equal(new[] { "a" }, await db.Parcels.AsNoTracking().Where(p => p.Local == local).Select(p => p.Name).ToListAsync());
        }
    }

    [PostGisFact]
    public void EfCore_ModelIsStableAcrossBuilds()
    {
        // Regression: HasData geometries must compare structurally, or the migrations differ sees a change
        // every time the model is built (PendingModelChangesWarning on `dotnet ef database update`).
        using var first = new ParcelContext(_fixture.ConnectionString);
        using var second = new ParcelContext(_fixture.ConnectionString);

        var seedProperty = first.Model.FindEntityType(typeof(Parcel))!.FindProperty(nameof(Parcel.Shape))!;
        var comparer = seedProperty.GetTypeMapping().ProviderValueComparer;

        Assert.True(comparer.Equals(ParcelContext.SeedShape, Geometry<Point>.FromWkt("POINT (51.4 35.7)", 4326)));
        Assert.Equal(
            first.Database.GenerateCreateScript(),
            second.Database.GenerateCreateScript());
    }

    #endregion
}
