using System; 
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Helpers;
 

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

    private string? _iconHref;
    public string? IconHref
    {
        get => _iconHref;
        set
        {
            _iconHref = value;
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

    public void EnsureIconLoaded()
    {
        if (IconHref.IsNullOrEmpty() || (ImageSymbol != null && ImageSymbolGdiPlus != null))
        {
            return;
        }

        try
        {
            var bitmap = LoadBitmap();
            if (bitmap != null)
            {
                bitmap.Freeze();
                ImageSymbol ??= bitmap;
                ImageSymbolGdiPlus ??= bitmap.AsGdiPlusImage();
            }
        }
        catch
        {
            // Ignore loading failures; fall back to default rendering
        }
    }

    private BitmapImage? LoadBitmap()
    {
        if (IconHref.IsNullOrEmpty())
        {
            return null;
        }

        if (Uri.TryCreate(IconHref, UriKind.Absolute, out var absolute))
        {
            return ImageUtility.CreateBitmapImage(absolute);
        }

        var fullPath = Path.GetFullPath(IconHref);
        if (File.Exists(fullPath))
        {
            return ImageUtility.CreateBitmapImage(new Uri(fullPath, UriKind.Absolute));
        }

        return null;
    }
      
}
