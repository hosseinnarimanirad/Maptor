
namespace IRI.Maptor.Core.Common.Primitives;

public static class BoundingBoxes
{
    public static BoundingBox Mercator_Iran => new BoundingBox(4840000, 2800000, 7080000, 4900000);

    //Same as Mercator
    public static BoundingBox WebMercator_Iran => new BoundingBox(xMin: 4840000, yMin: 2800000, xMax: 7080000, yMax: 4900000);

    public static BoundingBox GeodeticWgs84_Iran => new BoundingBox(44, 24.5, 63.5, 40);


    public static BoundingBox WebMercator_Africa => new BoundingBox(xMin: -2_000_000, yMin: -4_000_000, xMax: 6_000_000, yMax: 5_000_000);
    public static BoundingBox WebMercator_Europe => new BoundingBox(xMin: -2_000_000, yMin: +5_000_000, xMax: 5_000_000, yMax: 11_000_000);
    public static BoundingBox WebMercator_SouthAmerica => new BoundingBox(xMin: -9_000_000, yMin: -8_000_000, xMax: -4_000_000, yMax: 2_000_000);
    public static BoundingBox WebMercator_NorthAmerica => new BoundingBox(xMin: -14_000_000, yMin: 2_000_000, xMax: -6_000_000, yMax: 11_000_000);
}
