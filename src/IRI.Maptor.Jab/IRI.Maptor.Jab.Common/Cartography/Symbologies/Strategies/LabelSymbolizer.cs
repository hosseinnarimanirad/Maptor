using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public class LabelSymbolizer : SymbolizerBase
{
    public override SymbologyType Type =>  SymbologyType.Label;
      

    public LabelSymbolizer(VisualParameters labels)
    {
        Param = labels;
    } 
}
