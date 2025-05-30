﻿namespace IRI.Sta.Common.Primitives;

public static class BoundingBoxes
{
    public static BoundingBox IranMercatorBoundingBox
    {
        get
        {
            return new BoundingBox(4840000, 2800000, 7080000, 4900000);
        }
    }

    //Same as Mercator
    public static BoundingBox IranWebMercatorBoundingBox
    {
        get
        {
            return new BoundingBox(xMin: 4840000, yMin: 2800000, xMax: 7080000, yMax: 4900000);
        }
    }

    public static BoundingBox IranGeodeticWgs84BoundingBox
    {
        get
        {
            return new BoundingBox(44, 24.5, 63.5, 40);
        }
    }
}
