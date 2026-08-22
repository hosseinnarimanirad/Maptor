namespace IRI.Maptor.Infrastructure.PersonalGdb.Write;

// Grid math for the per-feature-class '<Name>_SHAPE_Index' side table. ArcGIS reads the
// grid origin and size back from GDB_GeomColumns (IdxOriginX/Y, IdxGridSize), so the only
// hard requirement is that the rows written here stay consistent with those stored values.
internal static class PersonalGdbSpatialIndex
{
    // ArcGIS default for geographic CRSs: the -400..400 degree domain split into 2^31 cells
    // (verified numerically against an ArcGIS-authored pgdb)
    internal const double GeographicGridSize = 800.0 / 2147483648.0;

    // meter-based default; coordinates are offset from the -5120900/-9998100 domain origin,
    // so 1 km cells keep cell numbers well inside Int32 for real-world coordinates
    internal const double ProjectedGridSize = 1000.0;

    internal static int GetGridCell(double coordinate, double origin, double gridSize)
        => (int)Math.Floor((coordinate - origin) / gridSize);
}
