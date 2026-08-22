namespace IRI.Maptor.Infrastructure.PersonalGdb.Enums;
 
public enum GdbEsriFieldType
{
    // 	16-bit Integer.
    esriFieldTypeSmallInteger = 2,

    // 	32-bit Integer.
    esriFieldTypeInteger = 3,

    // 	Single-precision floating-point number.
    esriFieldTypeSingle = 4,

    //	Double-precision floating-point number.
    esriFieldTypeDouble = 5,

    // 	Character string.
    esriFieldTypeString = 130,

    // 	Date.
    esriFieldTypeDate = 133,

    // 	Integer representing an object identifier. 32-bit OID has a length of 4 bytes, and 64-bit OID has a length of 8 bytes.
    esriFieldTypeOID = 1000,

    // 	Geometry.
    esriFieldTypeGeometry = 1001,

    // 	Binary Large Object.
    esriFieldTypeBlob = 128,

    // 	Raster.
    esriFieldTypeRaster = 1003,

    // 	Globally Unique Identifier.
    esriFieldTypeGUID = 72,

    // 	Esri Global ID.
    esriFieldTypeGlobalID = 1005,

    // 	XML Document.
    esriFieldTypeXML = 1006,

    // 	64-bit Integer.
    esriFieldTypeBigInteger = 13,
}
