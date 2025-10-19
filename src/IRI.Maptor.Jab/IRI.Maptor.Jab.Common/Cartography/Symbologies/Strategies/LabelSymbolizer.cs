using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public class LabelSymbolizer : SymbolizerBase
{
    public override SymbologyType Type =>  SymbologyType.Label;
      
    public string LabelAttribute { get; set; }

    public LabelSymbolizer(VisualParameters labels, string labelAttribute)
    {
        Param = labels;
        LabelAttribute = labelAttribute;
    } 
}
