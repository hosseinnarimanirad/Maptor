using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Infrastructure.SqlServer;

public class SqlServerSourceParameter
{
    public string ConnectionString { get; set; }
    
    public string TableName { get; set; }

    public string SpatialColumnName { get; set; }

    public string LabelColumn { get; set; }

    public string QueryString { get; set; }

    public SqlServerSourceParameter(string connectionString, string tableName, string spatialColumnName, string labelColumn, string queryString)
    {
        ConnectionString = connectionString;

        TableName = tableName;

        SpatialColumnName = spatialColumnName;

        LabelColumn = labelColumn;

        QueryString = queryString;
    }
}