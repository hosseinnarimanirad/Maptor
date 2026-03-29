using IRI.Maptor.Sta.Common.Attributes;
using IRI.Maptor.Sta.Persistence.Abstractions;
using IRI.Maptor.Sta.Persistence.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class DataSourceKindExtensions
{
    public static DataSourceCategory GetCategory(this DataSourceKind kind)
    {
        var attribute = kind.GetAttribute<DataSourceKindInfoAttribute>();

        return attribute?.Category ?? DataSourceCategory.None;
    }
}
