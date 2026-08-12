using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Wpf.Localization;

public static class LocalizationManagerWpfExtensions
{
    public static FlowDirection CurrentFlowDirection(this LocalizationManager manager)
        => manager.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

public class IsRightToLeftToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
