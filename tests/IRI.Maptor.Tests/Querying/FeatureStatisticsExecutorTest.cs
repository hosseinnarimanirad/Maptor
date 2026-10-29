using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Infrastructure.EfCore.Extensions;
using IRI.Maptor.Infrastructure.EfCore.Querying;
using IRI.Maptor.Infrastructure.EfCore.SqlServer.Extensions;
using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;
using IRI.Maptor.Presentation.Wpf.Models.LayerSettings;
using IRI.Maptor.Tests.PostgreSql;

using Point = IRI.Maptor.Core.Common.Primitives.Point;

namespace IRI.Maptor.Tests.Querying;

/// <summary>
/// The server's whole-layer statistics (<see cref="FeatureStatisticsExecutor"/>), read through the client's mapping
/// (<see cref="ServerStatistics"/>), must give what the in-memory calculators give on the same rows. Runs on a
/// throwaway SQL Server LocalDB database and on PostGIS (each skipped when unavailable). Synthetic data only.
/// </summary>
public class FeatureStatisticsExecutorTest
{
    public class Pipe
    {
        public int Id { get; set; }

        public string? Material { get; set; }

        public double? Diameter { get; set; }

        public int Floors { get; set; }

        public DateTime? Built { get; set; }

        public bool Active { get; set; }

        public Geometry<Point> Shape { get; set; } = null!;
    }

    private sealed class PipeContext(Action<DbContextOptionsBuilder> configure) : DbContext
    {
        public DbSet<Pipe> Pipes => Set<Pipe>();

        protected override void OnConfiguring(DbContextOptionsBuilder options) => configure(options);

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
            => configurationBuilder.ConfigureMaptorGeometry("GEOGRAPHY");    // as Barg/Gas configure it

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var pipe = modelBuilder.Entity<Pipe>();
            pipe.ToTable("pipes").Property(p => p.Id).ValueGeneratedNever();

            // as Gas maps dates on PostgreSQL (the equivalent of datetime2)
            if (Database.ProviderName?.Contains("Npgsql") == true)
                pipe.Property(p => p.Built).HasColumnType("timestamp without time zone");
        }
    }

    private const string LocalDbServer = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";

    private static readonly Lazy<bool> LocalDbAvailable = new(() =>
    {
        try
        {
            using var connection = new SqlConnection(LocalDbServer + ";Connect Timeout=5");
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    });

    // ~1 km lines, small squares (CCW, as geography wants) and a point: the measure of one kind leaves the others unmeasured
    private static List<Pipe> CreatePipes()
    {
        var random = new Random(7);
        var materials = new[] { "PE", "Steel", "Cast iron", null, "  " };
        var pipes = new List<Pipe>();

        for (int i = 1; i <= 300; i++)
        {
            double x = 51.3 + random.NextDouble() * 0.2, y = 35.6 + random.NextDouble() * 0.2;

            var wkt = (i % 10) switch
            {
                0 => FormattableString.Invariant($"POLYGON (({x} {y}, {x + 0.01} {y}, {x + 0.01} {y + 0.01}, {x} {y + 0.01}, {x} {y}))"),
                5 => FormattableString.Invariant($"POINT ({x} {y})"),
                _ => FormattableString.Invariant($"LINESTRING ({x} {y}, {x + 0.005} {y + 0.003}, {x + 0.011} {y + 0.002})"),
            };

            pipes.Add(new Pipe
            {
                Id = i,
                Material = materials[random.Next(materials.Length)],
                Diameter = i % 13 == 0 ? null : Math.Round(50 + random.NextDouble() * 350, 1),
                Floors = random.Next(10),
                Built = i % 11 == 0 ? null : new DateTime(2000, 1, 1).AddDays(random.Next(9000)).AddSeconds(random.Next(86400)),
                Active = random.Next(3) > 0,
                Shape = Geometry<Point>.FromWkt(wkt, 4326),
            });
        }

        return pipes;
    }

    private static List<Feature<Point>> ToFeatures(IEnumerable<Pipe> pipes) => pipes.Select(p => new Feature<Point>(p.Shape, new Dictionary<string, object>
    {
        [nameof(Pipe.Material)] = p.Material!,
        [nameof(Pipe.Diameter)] = p.Diameter!,
        [nameof(Pipe.Floors)] = p.Floors,
        [nameof(Pipe.Built)] = p.Built!,
        [nameof(Pipe.Active)] = p.Active,
    })).ToList();

    private static List<Field> GetFields()
    {
        var fields = Field.GetFields<Pipe>();

        // a domain on Material: "Cast iron" is outside it
        fields.Single(f => f.Name == nameof(Pipe.Material)).Domain = FieldDomain.FromValues(new object?[] { "PE", "Steel" });

        return fields;
    }

    [Fact]
    public async Task SqlServer_MatchesTheInMemoryCalculators()
    {
        if (!LocalDbAvailable.Value)
            return;    // no LocalDB on this machine: nothing to check

        var database = "maptor_stats_" + Guid.NewGuid().ToString("N")[..12];
        var connectionString = $"{LocalDbServer};Database={database}";

        await RunAsync(() => new PipeContext(o => o.UseSqlServer(connectionString, x => x.UseMaptor())));
    }

    [PostGisFact]
    public async Task PostGis_MatchesTheInMemoryCalculators()
    {
        var database = "maptor_tests_" + Guid.NewGuid().ToString("N")[..12];
        var connectionString = await PostGisTestDatabase.CreateDatabaseAsync(database);

        try
        {
            await RunAsync(() => new PipeContext(o => o.UseNpgsql(connectionString, x => x.UseMaptor())), createDatabase: false);
        }
        finally
        {
            NpgsqlConnectionPoolClear();
            await PostGisTestDatabase.DropDatabaseAsync(database);
        }
    }

    private static void NpgsqlConnectionPoolClear() => Npgsql.NpgsqlConnection.ClearAllPools();

    [Fact]
    public void Json_RoundTrip_GivesTheSameResultsAsTheServerObjects()
    {
        // what ASP.NET writes (web defaults) and what the client reads (JsonHelper.DefaultOptions): dates arrive as
        // text, integers as int, so the mapping must normalise them the way it does feature values
        var web = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);

        T RoundTrip<T>(T dto) => System.Text.Json.JsonSerializer.Deserialize<T>(
            System.Text.Json.JsonSerializer.Serialize(dto, web), IRI.Maptor.Core.Common.Helpers.JsonHelper.DefaultOptions)!;

        var previous = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            // the Persian culture parses dates on its own calendar: the mapping must not depend on it
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fa-IR");

            var fields = GetFields();

            var built = fields.Single(f => f.Name == nameof(Pipe.Built));
            var dates = new FieldStatisticsDto
            {
                FeatureCount = 3,
                EmptyCount = 1,
                DistinctCount = 1,
                MinDate = new DateTime(2024, 3, 1, 10, 30, 0),
                MaxDate = new DateTime(2024, 3, 1, 10, 30, 0),
                Values = new object?[] { new DateTime(2024, 3, 1, 10, 30, 0) },
                Counts = new[] { 2 },
            };

            var fromJson = ServerStatistics.ToFieldStatistics(RoundTrip(dates), built);
            Assert.Equal(ServerStatistics.ToFieldStatistics(dates, built).TopValues.Select(v => (v.Value, v.Count)), fromJson.TopValues.Select(v => (v.Value, v.Count)));
            Assert.Equal(new DateTime(2024, 3, 1, 10, 30, 0), fromJson.TopValues[0].Value);

            var floors = fields.Single(f => f.Name == nameof(Pipe.Floors));
            var groups = new GroupStatisticsDto
            {
                FeatureCount = 5,
                MeasureKind = GeometryMeasureKinds.Length,
                Keys = new object?[] { 3, 7L },
                Counts = new[] { 3, 2 },
                Measures = new double?[] { 1200.5, null },
                Sums = new double?[] { null, 4 },
                TotalMeasure = 1200.5,
                TotalSum = 4,
                GroupCount = 2,
            };

            var group = ServerStatistics.ToGroupStatistics(RoundTrip(groups), floors);
            Assert.Equal(new object?[] { 3d, 7d }, group.Rows.Select(r => r.Key));
            Assert.Equal(new double?[] { 1200.5, 0 }, group.Rows.Select(r => r.Measure));
            Assert.Equal(GeometryMeasureKind.Length, group.MeasureKind);

            var geometry = ServerStatistics.ToGeometryStatistics(RoundTrip(new GeometryStatisticsDto
            {
                FeatureCount = 4,
                MeasureKind = GeometryMeasureKinds.Area,
                Measures = new NumericSummaryDto { Count = 2, Min = 1, Max = 3, Sum = 4, Mean = 2, Median = 2, Histogram = new HistogramDto { Start = 0, BinWidth = 2, Counts = new[] { 1, 1 } } },
            }));
            Assert.Equal(new[] { 1, 1 }, geometry.Measures!.Histogram!.Counts);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    private static async Task RunAsync(Func<PipeContext> createContext, bool createDatabase = true)
    {
        var pipes = CreatePipes();

        await using (var setup = createContext())
        {
            // SQL Server: creates the database too; PostGIS: the tables in the database the fixture made
            await setup.Database.EnsureCreatedAsync();

            setup.Pipes.AddRange(pipes);
            await setup.SaveChangesAsync();
        }

        try
        {
            await using var db = createContext();

            var features = ToFeatures(pipes);
            var fields = GetFields();
            var limits = new FeatureQueryLimits();

            foreach (var field in fields.Where(f => f.Name != nameof(Pipe.Id) && f.Name != nameof(Pipe.Shape)))
            {
                var dto = await FeatureStatisticsExecutor.FieldStatisticsAsync<Pipe>(db, field.Name, limits);

                AssertSame(FieldStatistics.Calculate(features, field), ServerStatistics.ToFieldStatistics(dto, field), field.Name);
            }

            foreach (var kind in new[] { GeometryMeasureKind.Length, GeometryMeasureKind.Area, GeometryMeasureKind.None })
            {
                var measures = kind == GeometryMeasureKind.None ? null : GeometryMeasure.Calculate(features, kind);

                var geometry = await FeatureStatisticsExecutor.GeometryStatisticsAsync<Pipe>(db, ServerStatistics.ToServerMeasureKind(kind));

                AssertSame(GeometryStatistics.Calculate(features, kind, measures), ServerStatistics.ToGeometryStatistics(geometry), kind.ToString());

                foreach (var (group, sum) in new[] { ("Material", "Diameter"), ("Floors", (string?)null), ("Active", "Floors") })
                {
                    var groupField = fields.Single(f => f.Name == group);
                    var sumField = sum is null ? null : fields.Single(f => f.Name == sum);

                    var dto = await FeatureStatisticsExecutor.GroupStatisticsAsync<Pipe>(db, group, sum, ServerStatistics.ToServerMeasureKind(kind), limits);

                    AssertSame(GroupStatistics.Calculate(features, groupField, kind, measures, sumField), ServerStatistics.ToGroupStatistics(dto, groupField), $"{group}/{kind}");
                }
            }

            // the caps: only the most frequent values / largest groups come back, with the full counts beside them
            var capped = new FeatureQueryLimits { MaxDistinctValues = 3 };

            var diameter = await FeatureStatisticsExecutor.FieldStatisticsAsync<Pipe>(db, nameof(Pipe.Diameter), capped);
            Assert.True(diameter.Truncated);
            Assert.Equal(3, diameter.Values.Length);
            Assert.True(diameter.DistinctCount > 3);

            var floors = ServerStatistics.ToGroupStatistics(await FeatureStatisticsExecutor.GroupStatisticsAsync<Pipe>(db, nameof(Pipe.Floors), null, null, capped), fields.Single(f => f.Name == nameof(Pipe.Floors)));
            Assert.Equal(3, floors.Rows.Count);
            Assert.Equal(10, floors.GroupCount);

            await Assert.ThrowsAsync<FeatureQueryException>(() => FeatureStatisticsExecutor.FieldStatisticsAsync<Pipe>(db, "Nope", limits));
            await Assert.ThrowsAsync<FeatureQueryException>(() => FeatureStatisticsExecutor.GroupStatisticsAsync<Pipe>(db, "Floors", "Material", null, limits));
            await Assert.ThrowsAsync<FeatureQueryException>(() => FeatureStatisticsExecutor.GeometryStatisticsAsync<Pipe>(db, "Volume"));
        }
        finally
        {
            if (createDatabase)
            {
                await using var cleanup = createContext();
                await cleanup.Database.EnsureDeletedAsync();
            }
        }
    }

    /// <summary>A Live source whose server answers are canned: the tab must ask the server, never compute in memory.</summary>
    private sealed class FakeLiveSource(IEnumerable<Feature<Point>> features)
        : IRI.Maptor.Core.Persistence.DataSources.MemoryDataSource(features), IRI.Maptor.Core.Persistence.Abstractions.IServerQueryableVectorDataSource
    {
        public List<string> Calls { get; } = new();

        public bool CanQueryServer => true;

        public bool IsViewTruncated => false;

        public event EventHandler<bool>? ViewTruncatedChanged { add { } remove { } }

        public Task<IRI.Maptor.Core.Persistence.Abstractions.FeatureQueryPage> QueryPageAsync(FeatureQueryDto query, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new IRI.Maptor.Core.Persistence.Abstractions.FeatureQueryPage { TotalCount = 4_000_000 });

        public Task<DistinctValuesDto> GetDistinctValuesAsync(string fieldName, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new DistinctValuesDto());

        public Task<FieldStatisticsDto> GetFieldStatisticsAsync(string fieldName, System.Threading.CancellationToken cancellationToken = default)
        {
            Calls.Add($"field {fieldName}");

            return Task.FromResult(new FieldStatisticsDto { FeatureCount = 4_000_000, EmptyCount = 10, DistinctCount = 2, Values = new object?[] { "PE", "Steel" }, Counts = new[] { 3_000_000, 999_990 } });
        }

        public Task<GroupStatisticsDto> GetGroupStatisticsAsync(string fieldName, string? sumFieldName, string? measureKind, System.Threading.CancellationToken cancellationToken = default)
        {
            Calls.Add($"group {fieldName} {sumFieldName} {measureKind}");

            return Task.FromResult(new GroupStatisticsDto
            {
                FeatureCount = 4_000_000,
                MeasureKind = measureKind,
                Keys = new object?[] { "PE" },
                Counts = new[] { 3_000_000 },
                Measures = new double?[] { 5_000 },
                Sums = new double?[] { null },
                TotalMeasure = 6_000,
                GroupCount = 1_500,
                Truncated = true,
            });
        }

        public Task<GeometryStatisticsDto> GetGeometryStatisticsAsync(string? measureKind, System.Threading.CancellationToken cancellationToken = default)
        {
            Calls.Add($"geometry {measureKind}");

            return Task.FromResult(new GeometryStatisticsDto { FeatureCount = 4_000_000, MeasureKind = measureKind });
        }
    }

    [Fact]
    public void StatisticsTab_LiveLayer_AsksTheServer()
    {
        var line = Geometry<Point>.FromWkt("LINESTRING (5720000 4260000, 5721000 4261000)", 3857);

        var source = new FakeLiveSource(new[] { new Feature<Point>(line, new Dictionary<string, object> { ["Material"] = "PE", ["Diameter"] = 110.0 }) });

        var layer = new IRI.Maptor.Presentation.Wpf.Layers.VectorLayer("pipes", source,
            new IRI.Maptor.Presentation.Wpf.VisualParameters(null, System.Windows.Media.Brushes.Red, 1, 1),
            IRI.Maptor.Presentation.Core.LayerType.VectorLayer, IRI.Maptor.Presentation.Core.RenderMode.Default,
            IRI.Maptor.Presentation.Core.RasterizationMethod.DrawingVisual, IRI.Maptor.Presentation.Core.Models.ScaleInterval.All);

        var tab = new IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings.LayerSettings_StatisticsViewModel(layer);

        Assert.True(tab.IsLiveLayer);
        Assert.True(tab.CanCompute);

        tab.SelectedField = tab.Fields.Single(f => f.Field?.Name == "Material");
        tab.ComputeCommand.Execute(null);
        Assert.Null(tab.ErrorMessage);
        Assert.True(tab.HasResult);
        Assert.Contains(tab.Tiles, t => t.Value == "4,000,000");

        tab.Mode = IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings.StatisticsMode.GroupBy;
        tab.ComputeCommand.Execute(null);
        Assert.Contains(tab.Tiles, t => t.Value == "1,500");              // all groups, not the rows sent
        Assert.Contains("1,500", tab.ResultNote);

        tab.Mode = IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings.StatisticsMode.Geometry;
        tab.ComputeCommand.Execute(null);
        Assert.Null(tab.ErrorMessage);

        Assert.Equal(new[] { "field Material", "group Material  Length", "geometry Length" }, source.Calls);

        // the General tab counts a Live layer on the server too
        var general = new IRI.Maptor.Presentation.Wpf.ViewModels.LayerSettings.LayerSettings_GeneralSummaryViewModel(layer);
        Assert.Equal("4,000,000", general.FeatureCountText);
    }

    private static void AssertSame(FieldStatistics expected, FieldStatistics actual, string what)
    {
        Assert.True(expected.Kind == actual.Kind, $"{what}: kind");
        Assert.True(expected.FeatureCount == actual.FeatureCount, $"{what}: count");
        Assert.True(expected.EmptyCount == actual.EmptyCount, $"{what}: empty {expected.EmptyCount} vs {actual.EmptyCount}");
        Assert.True(expected.DistinctCount == actual.DistinctCount, $"{what}: distinct {expected.DistinctCount} vs {actual.DistinctCount}");
        Assert.True(expected.OutOfDomainCount == actual.OutOfDomainCount, $"{what}: out of domain");
        Assert.True(expected.UsedDomainValueCount == actual.UsedDomainValueCount, $"{what}: used domain values");
        Assert.True(expected.MinDate == actual.MinDate && expected.MaxDate == actual.MaxDate, $"{what}: dates {expected.MinDate}..{expected.MaxDate} vs {actual.MinDate}..{actual.MaxDate}");

        AssertClose(expected.Min, actual.Min, 1e-9, $"{what}: min");
        AssertClose(expected.Max, actual.Max, 1e-9, $"{what}: max");
        AssertClose(expected.Sum, actual.Sum, 1e-9, $"{what}: sum");
        AssertClose(expected.Mean, actual.Mean, 1e-9, $"{what}: mean");
        AssertClose(expected.Median, actual.Median, 1e-9, $"{what}: median");
        AssertClose(expected.StandardDeviation, actual.StandardDeviation, 1e-9, $"{what}: standard deviation");
        AssertSame(expected.Histogram, actual.Histogram, $"{what}: histogram");

        Assert.Equal(expected.TopValues.Select(v => (v.Value, v.Count, v.IsOutOfDomain)), actual.TopValues.Select(v => (v.Value, v.Count, v.IsOutOfDomain)));
    }

    private static void AssertSame(GroupStatistics expected, GroupStatistics actual, string what)
    {
        Assert.True(expected.FeatureCount == actual.FeatureCount, $"{what}: count");
        Assert.True(expected.MeasureKind == actual.MeasureKind, $"{what}: measure kind");
        Assert.True(expected.OutOfDomainCount == actual.OutOfDomainCount, $"{what}: out of domain");
        Assert.True(expected.GroupCount == actual.GroupCount, $"{what}: groups");

        AssertClose(expected.TotalMeasure, actual.TotalMeasure, 5e-3, $"{what}: total measure");
        AssertClose(expected.TotalSum, actual.TotalSum, 1e-9, $"{what}: total sum");

        Assert.Equal(expected.Rows.Select(r => (r.Key, r.Count, r.IsOutOfDomain)), actual.Rows.Select(r => (r.Key, r.Count, r.IsOutOfDomain)));

        foreach (var (e, a) in expected.Rows.Zip(actual.Rows))
        {
            AssertClose(e.Measure, a.Measure, 5e-3, $"{what}: measure of {e.Key}");
            AssertClose(e.Sum, a.Sum, 1e-9, $"{what}: sum of {e.Key}");
        }
    }

    private static void AssertSame(GeometryStatistics expected, GeometryStatistics actual, string what)
    {
        Assert.True(expected.FeatureCount == actual.FeatureCount, $"{what}: count");
        Assert.True(expected.EmptyGeometryCount == actual.EmptyGeometryCount, $"{what}: empty");
        Assert.True(expected.MeasureKind == actual.MeasureKind, $"{what}: kind");
        Assert.True(expected.UnmeasuredCount == actual.UnmeasuredCount, $"{what}: unmeasured {expected.UnmeasuredCount} vs {actual.UnmeasuredCount}");
        Assert.True((expected.Measures is null) == (actual.Measures is null), $"{what}: measures");

        if (expected.Measures is { } e && actual.Measures is { } a)
        {
            // geodesic lengths/areas of two implementations: close, not equal; a value near a bin edge may change bins
            Assert.Equal(e.Count, a.Count);
            AssertClose(e.Sum, a.Sum, 5e-3, $"{what}: sum");
            AssertClose(e.Median, a.Median, 5e-3, $"{what}: median");
            AssertClose(e.Min, a.Min, 5e-3, $"{what}: min");
            AssertClose(e.Max, a.Max, 5e-3, $"{what}: max");
            Assert.Equal(e.Count, a.Histogram!.Counts.Sum());
        }
    }

    private static void AssertSame(Histogram? expected, Histogram? actual, string what)
    {
        Assert.True((expected is null) == (actual is null), what);

        if (expected is null || actual is null)
            return;

        AssertClose(expected.Start, actual.Start, 1e-9, $"{what}: start");
        AssertClose(expected.BinWidth, actual.BinWidth, 1e-9, $"{what}: width");
        Assert.Equal(expected.Counts, actual.Counts);
    }

    private static void AssertClose(double? expected, double? actual, double relative, string what)
    {
        Assert.True(expected.HasValue == actual.HasValue, $"{what}: {expected} vs {actual}");

        if (expected is double e && actual is double a)
            Assert.True(Math.Abs(e - a) <= relative * Math.Max(1, Math.Max(Math.Abs(e), Math.Abs(a))), $"{what}: {e} vs {a}");
    }
}
