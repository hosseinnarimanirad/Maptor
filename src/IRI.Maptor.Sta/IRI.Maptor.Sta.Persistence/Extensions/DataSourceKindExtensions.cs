using IRI.Maptor.Sta.Common.Attributes;
using IRI.Maptor.Sta.Common.Enums;

namespace IRI.Maptor.Extensions;

public static class DataSourceKindExtensions
{
    public static DataSourceCategory GetCategory(this DataSourceKind kind)
    {
        var attribute = kind.GetAttribute<DataSourceKindInfoAttribute>();

        return attribute?.Category ?? DataSourceCategory.None;
    }

    public static string GetFileFilter(this DataSourceKind kind)
    {
        var attribute = kind.GetAttribute<DataSourceKindInfoAttribute>();

        return attribute?.FileFilter ?? "*.*|*.*";
    }
}
