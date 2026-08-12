using System;
using System.Windows.Data;
using System.Security.Principal;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class IsAuthorizedToBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        IPrincipal principal = value as IPrincipal;

        if (value == null || parameter == null)
        {
            return false;
        }

        return principal.IsInRole(parameter.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

