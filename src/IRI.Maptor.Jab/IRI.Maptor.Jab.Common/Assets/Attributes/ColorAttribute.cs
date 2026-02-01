using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Assets.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ColorAttribute : Attribute
{
    public string HexColor { get; set; }

    public ColorAttribute(string hexColor)
    {
        HexColor = hexColor;
    }
}