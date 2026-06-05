using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Helpers;
using IRI.Maptor.Jab.Common.Converters;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Attributes;

namespace IRI.Maptor.Jab.Common.Behaviors;


public static class DataGridDictionaryBehavior
{
    public static readonly DependencyProperty GenerateColumnsFromDictionaryProperty =
        DependencyProperty.RegisterAttached(
            "GenerateColumnsFromDictionary",
            typeof(bool),
            typeof(DataGridDictionaryBehavior),
            new PropertyMetadata(false, OnGenerateColumnsFromDictionaryChanged));

    public static bool GetGenerateColumnsFromDictionary(DependencyObject obj) =>
        (bool)obj.GetValue(GenerateColumnsFromDictionaryProperty);

    public static void SetGenerateColumnsFromDictionary(DependencyObject obj, bool value) =>
        obj.SetValue(GenerateColumnsFromDictionaryProperty, value);

    private static void OnGenerateColumnsFromDictionaryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid grid)
        {
            if ((bool)e.NewValue)
            {
                grid.AutoGenerateColumns = false; // We control columns
                grid.Loaded -= Grid_Loaded;
                grid.Loaded += Grid_Loaded;
            }
            else
            {
                grid.Loaded -= Grid_Loaded;
            }
        }
    }

    public static void Regenerate(object sender)
    {
        Grid_Loaded(sender, new RoutedEventArgs());
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        var grid = sender as DataGrid;
        if (grid?.ItemsSource == null) return;

        // Clear existing generated columns
        grid.Columns.Clear();

        //var items = grid.ItemsSource as IEnumerable<Feature<IRI.Maptor.Sta.Common.Primitives.Point>>;
        //if (items == null) return;

        var presenter = grid.DataContext as SelectedLayer;

        if (presenter is null || presenter.Fields.IsNullOrEmpty())
            return;

        var keys = presenter.Fields.Select(a => a.Name).ToList();

        // Create editable columns bound to Attributes[key]
        foreach (var key in keys)
        {
            var field = presenter?.Fields?.FirstOrDefault(f => f.Name == key.ToString());

            if (field == null)
                continue;

            var fieldType = System.Type.GetType(field.TypeFullName);

            if (fieldType is null)
                continue;

            if (field.TypeFullName.ContainsIgnoreCase(FeatureTableHelper.NetTopologySuiteColumnName))
                continue;

            // todo: Consider adding a field in Field class to
            //       identify which fields can be shown on the
            //       attribute table
            if (field.Name.EqualsIgnoreCase("rowversion"))
                continue;

            if (!field.CanRead)
                continue;

            DataGridColumn? column = null;
            var isColumnReadOnly = !field.CanWrite;

            var typeName = field.TypeFullName; // e.g. "System.Int32"

            if (field.AllowedValues != null && field.AllowedValues.Length > 0)
            {
                column = new DataGridComboBoxColumn
                {
                    Header = field.Alias,
                    ItemsSource = field.AllowedValues,
                    SelectedItemBinding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                };
            }
            //if (string.Equals(typeName, "System.Boolean", StringComparison.OrdinalIgnoreCase))
            else if (fieldType.IsBool())
            {
                column = new DataGridCheckBoxColumn
                {
                    Header = field.Alias,
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                };
            }
            else if (fieldType.IsDateTime())
            //else if (string.Equals(typeName, "System.DateTime", StringComparison.OrdinalIgnoreCase))
            {
                column = new DataGridTemplateColumn
                {
                    Header = field.Alias,
                    CellTemplate = CreateDateDisplayTemplate($"Attributes[{key}]", field.DisplayFormat),
                    CellEditingTemplate = CreateDateEditingTemplate($"Attributes[{key}]")
                };
            }
            //else if (typeName.StartsWith("System.Int", StringComparison.OrdinalIgnoreCase) ||
            //         typeName.StartsWith("System.Decimal", StringComparison.OrdinalIgnoreCase) ||
            //         typeName.StartsWith("System.Double", StringComparison.OrdinalIgnoreCase) ||
            //         typeName.StartsWith("System.Single", StringComparison.OrdinalIgnoreCase))
            else if (fieldType.IsNumeric())
            {
                column = new DataGridTextColumn
                {
                    Header = field.Alias,
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        StringFormat = field.DisplayFormat ?? "0,0.####"
                    },
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                    },
                    EditingElementStyle = new Style(typeof(TextBox))
                    {
                        Setters = { new Setter(TextBox.TextAlignmentProperty, TextAlignment.Right) }
                    },
                };
            }
            //else if (string.Equals(typeName, "System.String", StringComparison.OrdinalIgnoreCase))
            else if (fieldType == typeof(string))
            {
                column = new DataGridTextColumn
                {
                    Header = field.Alias,
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    ElementStyle = CreateStringElementStyle(typeof(TextBlock), $"Attributes[{key}]", field.TextDirection),
                    EditingElementStyle = CreateStringElementStyle(typeof(TextBox), $"Attributes[{key}]", field.TextDirection)
                };
            }
            else
            {
                // Fallback
                column = new DataGridTextColumn
                {
                    Header = field.Alias,
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                };
            }

            if (column != null)
                column.IsReadOnly = isColumnReadOnly;

            grid.Columns.Add(column);
        }
    }

    private static DataTemplate CreateDateDisplayTemplate(string bindingPath, string? format = null)
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding(bindingPath)
        {
            Mode = BindingMode.OneWay,
            Converter = new LocalizedDateTimeConverter { Format = format }
        });
        template.VisualTree = factory;
        return template;
    }

    // Helper for DatePicker editing template
    private static DataTemplate CreateDateEditingTemplate(string bindingPath)
    {
        var template = new DataTemplate();

        var factory = new FrameworkElementFactory(typeof(DatePicker));
        factory.SetBinding(DatePicker.SelectedDateProperty, new Binding(bindingPath)
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });

        template.VisualTree = factory;
        return template;
    }

    /// <summary>
    /// Builds the element/editing style for a string column.
    /// When <paramref name="textDirection"/> is <see cref="FieldTextDirection.Auto"/> the existing
    /// content-sniffing converter is used (DataTrigger). When it is explicit the direction is fixed
    /// directly as a setter and the auto-detection trigger is omitted entirely.
    /// </summary>
    private static Style CreateStringElementStyle(Type targetType, string bindingPath, FieldTextDirection textDirection)
    {
        var style = new Style(targetType);

        if (textDirection == FieldTextDirection.Auto)
        {
            // Default LTR, switch to RTL when content is detected as RTL.
            style.Setters.Add(new Setter(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight));
            if (targetType == typeof(TextBlock))
                style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap));

            style.Triggers.Add(new DataTrigger
            {
                Binding = new Binding(bindingPath) { Converter = new RtlFlowDirectionConverter() },
                Value = FlowDirection.RightToLeft,
                Setters = { new Setter(FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft) }
            });
        }
        else
        {
            var direction = textDirection == FieldTextDirection.RightToLeft
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            style.Setters.Add(new Setter(FrameworkElement.FlowDirectionProperty, direction));
            if (targetType == typeof(TextBlock))
                style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap));
        }

        return style;
    }
}
