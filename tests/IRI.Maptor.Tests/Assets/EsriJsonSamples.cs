using IRI.Maptor.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Tests.Assets
{
    internal class EsriJsonSamples
    {
        internal const string point2DJson = "{\"x\" : -118.15, \"y\" : 33.80, \"type\" : \"point\", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string point3DJson = "{\"x\" : -118.15, \"y\" : 33.80, \"z\" : 10.0, \"type\" : \"point\", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string pointNullJson = "{\"x\" : null, \"type\" : \"point\", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string pointNaNJson = "{\"x\" : \"NaN\", \"y\" : 22.2, \"type\" : \"point\", \"spatialReference\" : {\"wkid\" : 4326}}";

        internal const string multipoint2DJson = "{\"points\" : [[-97.06138,32.837],[-97.06133,32.836],[-97.06124,32.834],[-97.06127,32.832]],\"type\" : \"multipoint \", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string multipoint3DJson = "{\"hasZ\" : true,\"points\" : [[-97.06138,32.837,35.0], [-97.06133,32.836,35.1], [-97.06124,32.834,35.2]],\"type\" : \"multipoint \", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string multipointEmptyJson = "{\"points\" : [  ], \"type\" : \"multipoint \", \"spatialReference\" : {\"wkid\" : 4326}}";

        internal const string polyline2DJson = "{\"paths\" : [[[-97.06138,32.837],[-97.06133,32.836],[-97.06124,32.834],[-97.06127,32.832]], [[-97.06326,32.759],[-97.06298,32.755]]],\"type\" : \"polyline\", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string polylineMJson = "{\"hasM\" : true,\"paths\" : [[[-97.06138,32.837,5],[-97.06133,32.836,6],[-97.06124,32.834,7],[-97.06127,32.832,8]],[[-97.06326,32.759],[-97.06298,32.755]]],\"type\" : \"polyline\", \"spatialReference\" : {\"wkid\" : 4326}}";
        internal const string polylineEmptyJson = "{\"paths\" : [ ],\"type\" : \"polyline\" }";

        internal const string polygon2DJson = "{\"rings\" : [[[-97.06138,32.837],[-97.06133,32.836],[-97.06124,32.834],[-97.06127,32.832],[-97.06138,32.837]],[[-97.06326,32.759],[-97.06298,32.755],[-97.06153,32.749],[-97.06326,32.759]]],\"spatialReference\" : {\"wkid\" : 4326},\"type\" : \"polygon \"}";
        internal const string polygon3DJson = "{\"hasZ\" : true,\"hasM\" : true,\"rings\" : [[[-97.06138,32.837,35.1,4],[-97.06133,32.836,35.2,4.1],[-97.06124,32.834,35.3,4.2],[-97.06127,32.832, 35.2,44.3],[-97.06138,32.837,35.1,4]],[[-97.06326,32.759,35.4],[-97.06298,32.755,35.5],[-97.06153,32.749,35.6],[-97.06326,32.759,35.4]]],\"spatialReference\" : {\"wkid\" : 4326},\"type\" : \"polygon \"}";
        internal const string polygonEmptyJson = "{\"rings\" : [ ],\"type\" : \"polygon \"}";

    }
}
