using IRI.Maptor.Core.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Attributes;


[AttributeUsage(AttributeTargets.Field)]
public class DataSourceKindInfoAttribute : Attribute
{
    public DataSourceCategory Category { get; set; }

    public string FileFilter { get; set; }
}
