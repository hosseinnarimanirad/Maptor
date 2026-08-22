using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Converters;

public class RootItemsSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var element = value as DependencyObject;
        if (element == null) return Binding.DoNothing;

        // Look for TreeView
        var treeView = FindAncestor<TreeView>(element);
        if (treeView != null) return treeView.ItemsSource;

        // Look for ListBox
        var listBox = FindAncestor<ListBox>(element);
        if (listBox != null) return listBox.ItemsSource;

        return Binding.DoNothing;
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t) return t;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
