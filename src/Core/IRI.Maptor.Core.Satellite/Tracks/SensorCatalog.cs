namespace IRI.Maptor.Core.Satellite.Tracks;

/// <summary>An imaging sensor's nadir swath.</summary>
public sealed class SensorInfo
{
    public SensorInfo(string name, double swathWidthKm)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A sensor name is required.", nameof(name));

        if (!(swathWidthKm > 0))
            throw new ArgumentOutOfRangeException(nameof(swathWidthKm), "The swath width must be positive.");

        Name = name;
        SwathWidthKm = swathWidthKm;
    }

    public string Name { get; }

    /// <summary>Full width across the track, km.</summary>
    public double SwathWidthKm { get; }

    public override string ToString() => $"{Name} ({SwathWidthKm:0} km)";
}

/// <summary>
/// Which satellites carry which imaging sensor, by NORAD catalog number. Starts with a few
/// well-known Earth-observation satellites; add your own with <see cref="Set"/>.
/// </summary>
public sealed class SensorCatalog
{
    private readonly Dictionary<int, SensorInfo> _sensors = new();

    /// <summary>A new catalog holding the built-in entries.</summary>
    public static SensorCatalog CreateDefault()
    {
        var catalog = new SensorCatalog();

        var oli = new SensorInfo("OLI", 185);
        catalog.Set(39084, oli);   // Landsat 8
        catalog.Set(49260, oli);   // Landsat 9

        var msi = new SensorInfo("MSI", 290);
        catalog.Set(40697, msi);   // Sentinel-2A
        catalog.Set(42063, msi);   // Sentinel-2B
        catalog.Set(60989, msi);   // Sentinel-2C

        catalog.Set(25994, new SensorInfo("MODIS", 2330));   // Terra

        return catalog;
    }

    public void Set(int catalogNumber, SensorInfo sensor) => _sensors[catalogNumber] = sensor ?? throw new ArgumentNullException(nameof(sensor));

    public bool Remove(int catalogNumber) => _sensors.Remove(catalogNumber);

    public bool TryGet(int catalogNumber, out SensorInfo sensor)
    {
        var found = _sensors.TryGetValue(catalogNumber, out var value);

        sensor = value!;

        return found;
    }

    public IReadOnlyDictionary<int, SensorInfo> All => _sensors;
}
