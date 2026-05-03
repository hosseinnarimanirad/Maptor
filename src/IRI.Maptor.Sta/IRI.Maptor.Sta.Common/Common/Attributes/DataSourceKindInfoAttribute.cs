using IRI.Maptor.Sta.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Attributes;


[AttributeUsage(AttributeTargets.Field)]
public class DataSourceKindInfoAttribute : Attribute
{
    public DataSourceCategory Category { get; set; }

    public string FileFilter { get; set; }
}
