using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace IRI.Maptor.Jab.Common.Assets.Fonts;

public static class IriFonts
{
    private static FontFamily _iranSans = new FontFamily(new Uri(@"pack://application:,,,/IRI.Maptor.Jab.Common;component/Assets/Fonts/IRANSans.ttf#IRANSans", UriKind.Absolute), "IRANSans");

    public static FontFamily IranSans
    {
        get { return _iranSans; }
    }
}
