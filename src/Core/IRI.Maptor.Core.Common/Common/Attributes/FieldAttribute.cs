using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FieldAttribute : Attribute
{
    public string Alias { get; set; }

	public int Length { get; set; }

	public bool CanRead { get; set; } = true;

	public bool CanWrite { get; set; } = true;

	public object[] AllowedValues { get; set; } = [];

	public string? DisplayFormat { get; set; }

	public FieldTextDirection TextDirection { get; set; } = FieldTextDirection.Auto;

	//public FieldAttribute(string alias)
	//{
	//	this.Alias = alias;
	//}
}
