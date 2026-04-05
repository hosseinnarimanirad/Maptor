using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class IriProvince93PathMarkupAttribute : Attribute
{
    public string PathMarkup { get; set; }

    public IriProvince93PathMarkupAttribute(string pathMarkup)
    {
        this.PathMarkup = pathMarkup;
    }
}
