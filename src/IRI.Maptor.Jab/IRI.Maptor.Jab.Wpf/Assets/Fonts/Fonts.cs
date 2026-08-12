using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Wpf.Assets.Fonts;

public static class IriFonts
{
    // The "./#family" form resolves the packaged ttf relative to the base URI. A bare family
    // name here would be treated as a SYSTEM font lookup — silently falling back to the default
    // font on machines where IRANSans is not installed.
    private static FontFamily _iranSans = new FontFamily(new Uri(@"pack://application:,,,/IRI.Maptor.Jab.Wpf;component/Assets/Fonts/", UriKind.Absolute), "./#IRANSans");

    public static FontFamily IranSans
    {
        get { return _iranSans; }
    }
}
