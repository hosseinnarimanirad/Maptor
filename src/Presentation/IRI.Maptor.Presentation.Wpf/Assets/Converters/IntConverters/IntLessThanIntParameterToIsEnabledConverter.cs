//using System;
//using System.Windows.Data;

//namespace IRI.Maptor.Presentation.Wpf.Converters;

//public class IntLessThanIntParameterToIsEnabledConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
//    {
//        int intValue = (int)value;

//        int maxValue = int.Parse(parameter.ToString());

//        return intValue < maxValue ? true : false;
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}
