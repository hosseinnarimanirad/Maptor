using System;
using System.Windows;
using System.Windows.Data;
using System.Security.Principal;

namespace IRI.Maptor.Jab.Common.Converters;

public class IsAuthorizedToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        IPrincipal principal = value as IPrincipal;

        if (value == null || parameter == null)
        {
            return Visibility.Collapsed;
        }

        return principal.IsInRole(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
