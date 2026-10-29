using System.Collections.Generic;

using IRI.Maptor.Core.Common.Enums;

namespace IRI.Maptor.Presentation.Wpf.Models.Legend;

/// <summary>
/// One category of the legend's data-source-kind filter (Vector, Raster, Service, ...) and its items.
/// The items are the same instances as in <c>LegendViewModel.DataSourceKindFilterItems</c>.
/// </summary>
public class DataSourceKindFilterGroup
{
    public DataSourceCategory Category { get; }

    public IReadOnlyList<DataSourceKindFilterItem> Items { get; }

    public DataSourceKindFilterGroup(DataSourceCategory category, IReadOnlyList<DataSourceKindFilterItem> items)
    {
        Category = category;
        Items = items;
    }
}
