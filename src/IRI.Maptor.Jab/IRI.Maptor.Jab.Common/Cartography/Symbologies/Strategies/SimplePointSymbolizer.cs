using System; 
using System.Windows.Media;
 

namespace IRI.Maptor.Jab.Common.Cartography.Symbologies;

public class SimplePointSymbolizer : SymbolizerBase
{
    public override SymbologyType Type { get => SymbologyType.Single; }

    private double _symbolWidth = 16;
    public double SymbolWidth
    {
        get => _symbolWidth;
        set
        {
            _symbolWidth = value;
            RaisePropertyChanged();
        }
    }

    private double _symbolHeight = 16;
    public double SymbolHeight
    {
        get => _symbolHeight;
        set
        {
            _symbolHeight = value;
            RaisePropertyChanged();
        }
    }


    private Geometry? _geometrySymbol;
    public Geometry? GeometrySymbol
    {
        get => _geometrySymbol;
        set
        {
            _geometrySymbol = value;
            RaisePropertyChanged();
        }
    }


    private ImageSource? _imageSymbol;
    public ImageSource? ImageSymbol
    {
        get => _imageSymbol;
        set
        {
            _imageSymbol = value;
            RaisePropertyChanged();
        }
    }


    private System.Drawing.Image? _imageSymbolGdiPlus;
    public System.Drawing.Image? ImageSymbolGdiPlus
    {
        get => _imageSymbolGdiPlus;
        set
        {
            _imageSymbolGdiPlus = value;
            RaisePropertyChanged();
        }
    }
     
    public SimplePointSymbolizer()
    {

    }

    public SimplePointSymbolizer(double pointSize)
    {
        SymbolHeight = pointSize;

        SymbolWidth = pointSize;
    }
      
}
