using System;
using System.Globalization;
using System.Windows.Data;
using IRI.Maptor.Sta.Common.Enums;
using MahApps.Metro.IconPacks;

namespace IRI.Maptor.Jab.Wpf.Converters;

public class FeatureStatusToMaterialIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FeatureStatus status)
        {
            return status switch
            {
                FeatureStatus.Unchanged => PackIconMaterialKind.Null,
                FeatureStatus.New => PackIconMaterialKind.Plus,
                FeatureStatus.Updated => PackIconMaterialKind.Pencil,
                FeatureStatus.Removed => PackIconMaterialKind.Minus,
                FeatureStatus.CanceledNew => PackIconMaterialKind.Minus,
                _ => PackIconMaterialKind.Null
            };
        }
        return PackIconMaterialKind.Null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
