using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.ColorBrushes;

namespace IRI.Maptor.Jab.Common.Converters;

public class FeatureStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureStatus status)
            return status switch
            {
                FeatureStatus.Unchanged => Brushes.Black,
                FeatureStatus.Updated => MapAppColors.OrangeBrush,
                FeatureStatus.New => MapAppColors.EmeraldBrush,
                FeatureStatus.Removed => MapAppColors.RedBrush,
                FeatureStatus.CanceledNew => MapAppColors.SteelBrush,

                _ => Brushes.DarkGray
            };

        return Brushes.DarkGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
