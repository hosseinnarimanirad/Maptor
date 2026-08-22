using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute : Attribute
{
    public string Alias { get; set; }

    public string Schema { get; set; }

    public string TableName { get; set; }
	 
}
