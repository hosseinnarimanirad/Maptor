using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IRI.Maptor.Core.Satellite.Sources;

namespace IRI.Maptor.Tests.Satellite;

/// <summary>The local element store, with a fake network and a fake clock: no request leaves the machine.</summary>
public sealed class ElementStoreTest : IDisposable
{
    private const string IssJson = @"[{
        ""OBJECT_NAME"": ""ISS (ZARYA)"", ""OBJECT_ID"": ""1998-067A"", ""EPOCH"": ""2008-09-20T12:25:40.104192"",
        ""MEAN_MOTION"": 15.72125391, ""ECCENTRICITY"": 0.0006703, ""INCLINATION"": 51.6416,
        ""RA_OF_ASC_NODE"": 247.4627, ""ARG_OF_PERICENTER"": 130.536, ""MEAN_ANOMALY"": 325.0288,
        ""CLASSIFICATION_TYPE"": ""U"", ""NORAD_CAT_ID"": 25544, ""BSTAR"": -1.1606e-5 }]";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "maptor-elementstore-" + Guid.NewGuid().ToString("N"));

    private DateTime _now = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

    private readonly FakeHandler _handler = new();

    private ElementStore CreateStore() => new(_folder, new CelesTrakClient(new HttpClient(_handler)), () => _now);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => Ok(IssJson);

        public List<Uri> Requests { get; } = new();

        public static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);

            return Task.FromResult(Respond(request));
        }
    }

    [Fact]
    public void Client_BuildsCelesTrakGpAddresses()
    {
        var client = new CelesTrakClient();

        Assert.Equal("https://celestrak.org/NORAD/elements/gp.php?GROUP=stations&FORMAT=JSON", client.GetAddress(CelesTrakQuery.Group("Stations")).ToString());
        Assert.Equal("https://celestrak.org/NORAD/elements/gp.php?CATNR=25544&FORMAT=JSON", client.GetAddress(CelesTrakQuery.CatalogNumber(25544)).ToString());
        Assert.Equal("catnr-25544", CelesTrakQuery.CatalogNumber(25544).Key);

        Assert.Throws<ArgumentException>(() => CelesTrakQuery.Group("a b"));
        Assert.Throws<ArgumentException>(() => CelesTrakQuery.Group("../x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CelesTrakQuery.CatalogNumber(0));
    }

    [Fact]
    public async Task FirstRequest_Downloads_AndKeepsACopy()
    {
        var result = await CreateStore().GetAsync(CelesTrakQuery.Group("stations"));

        Assert.Equal(ElementSource.Downloaded, result.Source);
        Assert.Equal("ISS (ZARYA)", Assert.Single(result.Elements).Name);
        Assert.Equal(_now, result.RetrievedUtc);
        Assert.Single(_handler.Requests);
        Assert.Contains("group-stations", CreateStore().Keys());
    }

    [Fact]
    public async Task WithinTwoHours_NothingIsDownloaded_EvenWhenForced()
    {
        var store = CreateStore();

        await store.GetAsync(CelesTrakQuery.Group("stations"));

        _now = _now.AddMinutes(90);

        var result = await store.GetAsync(CelesTrakQuery.Group("stations"), forceRefresh: true);

        Assert.Equal(ElementSource.Cache, result.Source);
        Assert.Contains("2 hours", result.Message);
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task AFreshCopy_IsUsed_UntilRefreshIsForced()
    {
        var store = CreateStore();

        await store.GetAsync(CelesTrakQuery.Group("stations"));

        _now = _now.AddHours(3);

        Assert.Equal(ElementSource.Cache, (await store.GetAsync(CelesTrakQuery.Group("stations"))).Source);
        Assert.Single(_handler.Requests);

        Assert.Equal(ElementSource.Downloaded, (await store.GetAsync(CelesTrakQuery.Group("stations"), forceRefresh: true)).Source);
        Assert.Equal(2, _handler.Requests.Count);
    }

    [Fact]
    public async Task AStaleCopy_IsReplaced()
    {
        var store = CreateStore();

        await store.GetAsync(CelesTrakQuery.Group("stations"));

        _now = _now.AddHours(13);

        var result = await store.GetAsync(CelesTrakQuery.Group("stations"));

        Assert.Equal(ElementSource.Downloaded, result.Source);
        Assert.Equal(_now, result.RetrievedUtc);
    }

    [Fact]
    public async Task Offline_FallsBackToTheLastCopy_OrSaysThereIsNone()
    {
        var store = CreateStore();

        await store.GetAsync(CelesTrakQuery.Group("stations"));

        var downloadedAt = _now;

        _now = _now.AddDays(2);

        _handler.Respond = _ => throw new HttpRequestException("No such host is known.");

        var result = await store.GetAsync(CelesTrakQuery.Group("stations"));

        Assert.Equal(ElementSource.CacheAfterFailedDownload, result.Source);
        Assert.Equal(downloadedAt, result.RetrievedUtc);
        Assert.Single(result.Elements);
        Assert.Contains("No such host", result.Message);

        var none = await store.GetAsync(CelesTrakQuery.Group("weather"));

        Assert.Equal(ElementSource.None, none.Source);
        Assert.Empty(none.Elements);
        Assert.Contains("no local copy", none.Message);
    }

    [Fact]
    public async Task AnEmptyAnswer_DoesNotReplaceAGoodCopy()
    {
        var store = CreateStore();

        await store.GetAsync(CelesTrakQuery.CatalogNumber(25544));

        _now = _now.AddDays(1);

        _handler.Respond = _ => FakeHandler.Ok("No GP data found");

        var result = await store.GetAsync(CelesTrakQuery.CatalogNumber(25544));

        Assert.Equal(ElementSource.CacheAfterFailedDownload, result.Source);
        Assert.Single(result.Elements);
        Assert.Contains(result.Errors, e => e.Contains("No GP data found"));

        // and the copy on disk is still the good one
        Assert.Single(store.ReadCached("catnr-25544")!.Elements);
    }

    [Fact]
    public async Task ServerErrors_AreTreatedLikeBeingOffline()
    {
        _handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        var result = await CreateStore().GetAsync(CelesTrakQuery.Group("stations"));

        Assert.Equal(ElementSource.None, result.Source);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Import_ReadsATleFile_AndKeepsIt()
    {
        Directory.CreateDirectory(_folder);

        var file = Path.Combine(_folder, "my satellites.tle");

        File.WriteAllText(file, "ISS\n" + OrbitalElementsReadingTest.IssLine1 + "\n" + OrbitalElementsReadingTest.IssLine2 + "\n");

        var store = CreateStore();

        var result = store.Import(file);

        Assert.Equal(ElementSource.Import, result.Source);
        Assert.Equal("ISS", Assert.Single(result.Elements).Name);
        Assert.Empty(_handler.Requests);

        Assert.Contains("import-my_satellites", store.Keys());
        Assert.Equal(ElementSource.Import, store.ReadCached("import-my_satellites")!.Source);
    }

    [Fact]
    public void Parse_TellsJsonFromTle_AndReportsNonsense()
    {
        Assert.Single(ElementStore.Parse(IssJson).Elements);
        Assert.Single(ElementStore.Parse(OrbitalElementsReadingTest.IssLine1 + "\n" + OrbitalElementsReadingTest.IssLine2).Elements);

        var (elements, errors) = ElementStore.Parse("[ not json");

        Assert.Empty(elements);
        Assert.Single(errors);
    }
}
