using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FieldAttribute : Attribute
{
    public string Alias { get; set; }

	public int Length { get; set; }

	public bool CanRead { get; set; } = true;

	public bool CanWrite { get; set; } = true;

	public string[] AllowedValues { get; set; } = Array.Empty<string>();

	//public FieldAttribute(string alias)
	//{
	//	this.Alias = alias;
	//}
}
