using IRI.Maptor.Sta.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Persistence.Attributes;


[AttributeUsage(AttributeTargets.Field)]
public class DataSourceKindInfoAttribute : Attribute
{
    public DataSourceCategory Category { get; set; }
}
