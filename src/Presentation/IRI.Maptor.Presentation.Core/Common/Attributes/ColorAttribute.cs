namespace IRI.Maptor.Presentation.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ColorAttribute : Attribute
{
    public string HexColor { get; set; }

    public ColorAttribute(string hexColor)
    {
        HexColor = hexColor;
    }
}