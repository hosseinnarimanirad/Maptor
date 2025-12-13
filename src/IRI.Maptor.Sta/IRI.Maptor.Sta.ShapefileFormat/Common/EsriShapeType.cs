// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

namespace IRI.Maptor.Sta.ShapefileFormat;

public enum EsriShapeType : int
{
    NullShape = 0,
    EsriPoint = 1,
    EsriPolyLine = 3,
    EsriPolygon = 5,
    EsriMultiPoint = 8,

    EsriPointZM = 11,
    EsriPolyLineZM = 13,
    EsriPolygonZM = 15,
    EsriMultiPointZM = 18,

    EsriPointM = 21,
    EsriPolyLineM = 23,
    EsriPolygonM = 25,
    EsriMultiPointM = 28,

    EsriMultiPatch = 31,

    // 1401.10.29
    // these codes wont be used in shapefile
    // they are used in personal GDB
    EsriPointZ = 9,
    EsriPolyLineZ = 10,
    EsriPolygonZ = 19,
    EsriMultiPointZ = 20,

}
