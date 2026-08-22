using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Extensions;

public static class FeatureExtensions
{
    public static DataTable AsDataTable(this List<Feature<Point>> list)
    {
        var dataTable = new DataTable();

        if (list.Count == 0)
            return dataTable;

        var columnNames = list.First().Attributes.Select(dict => dict.Key.ToString()).Distinct().ToList();

        dataTable.Columns.AddRange(columnNames.Select(c => new DataColumn(c)).ToArray());

        dataTable.Columns.Add("GeoCol", typeof(Microsoft.SqlServer.Types.SqlGeometry));

        for (int row = 0; row < list.Count; row++)
        {
            var dataRow = dataTable.NewRow();

            dataRow["GeoCol"] = list[row].TheGeometry;

            for (int col = 0; col < columnNames.Count; col++)
            {
                var value = list[row].Attributes[columnNames[col]];

                if (value == null)
                    continue;

                dataRow[col] = value;
            }

            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }
}
