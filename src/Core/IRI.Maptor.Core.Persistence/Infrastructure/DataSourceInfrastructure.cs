using System;
using System.Data;
using System.Collections.Generic;

namespace IRI.Maptor.Core.Persistence.Infrastructure;

//Remember; 
//What about e.g. decimal(5,2)
//What about geometries and geographies
public abstract class DataSourceInfrastructure
{
    protected Dictionary<Type, string> _typeMapping;

    public DataSourceInfrastructure()
    {
        _typeMapping = new Dictionary<Type, string>();
    }

    protected virtual string GetColumnDefinition(DataColumn column)
    {
        return string.Format("{0} {1} {2} {3}",
            column.ColumnName,
            GetColumnType(column.DataType, column.MaxLength),
            column.AllowDBNull ? "NULL" : "NOT NULL",
            column.Unique ? "UNIQUE" : string.Empty);
    }

    protected virtual string GetColumnType(Type type, int length)
    {
        switch (type.Name.ToLower())
        {
            case "sqlbytes":
            case "sqlchars":
                return string.Format("{0}(MAX)", _typeMapping[type]);

            //case "sqlgeometry":
            //case "sqlgeography":
            //    return string.Format("{0}", _typeMapping[type]);

            default:
                return _typeMapping[type];
        }
    }

    public virtual string GetProperCreateCommand(IEnumerable<DataColumn> columns, string tableName)
    {
        List<string> result = new List<string>();

        foreach (var item in columns)
        {
            result.Add(GetColumnDefinition(item));
        }

        return string.Format("CREATE TABLE {0}({1})", tableName, string.Join(",", result));
    }
}
