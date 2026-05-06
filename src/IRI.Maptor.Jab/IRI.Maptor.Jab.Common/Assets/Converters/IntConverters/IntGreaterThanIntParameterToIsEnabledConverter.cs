//using System;
//using System.Windows.Data;

//namespace IRI.Maptor.Jab.Common.Converters;

//public class IntGreaterThanIntParameterToIsEnabledConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
//    {
//        int intValue = (int)value;

//        int minValue = int.Parse(parameter.ToString());

//        return intValue > minValue ? true : false;
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}
