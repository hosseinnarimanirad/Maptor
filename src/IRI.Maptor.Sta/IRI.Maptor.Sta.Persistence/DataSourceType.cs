using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.Persistence;

public enum DataSourceType
{
    None = 1,
    Shapefile = 2,
    PostgreSQL = 3,
    MongoDb = 4,
    PersonalGdb = 5,
    SQLServer = 6,

    // deprecated
    SQLCompact = 7,
}
