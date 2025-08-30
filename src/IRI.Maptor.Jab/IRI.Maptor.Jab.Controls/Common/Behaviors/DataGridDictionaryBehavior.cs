using IRI.Maptor.Jab.Common.Models.Map;
using IRI.Maptor.Sta.Spatial.Primitives;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using IRI.Maptor.Extensions;
using Microsoft.VisualBasic.FileIO;
using System;
using IRI.Maptor.Jab.Common.Assets.Converters;

namespace IRI.Maptor.Jab.Controls.Common.Behaviors;


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
                grid.Loaded += Grid_Loaded;
            }
            else
            {
                grid.Loaded -= Grid_Loaded;
            }
        }
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        var grid = sender as DataGrid;
        if (grid?.ItemsSource == null) return;

        // Clear existing generated columns
        grid.Columns.Clear();

        var items = grid.ItemsSource as IEnumerable<Feature<IRI.Maptor.Sta.Common.Primitives.Point>>;
        if (items == null) return;

        var presenter = grid.DataContext as SelectedLayer;

        if (presenter is null || presenter.Fields.IsNullOrEmpty())
            return;

        var keys = items.First().Attributes.Select(a => a.Key).ToList();
         
        // Create editable columns bound to Attributes[key]
        foreach (var key in keys)
        {
            var field = presenter?.Fields?.FirstOrDefault(f => f.Name == key.ToString());

            if (field == null)
                continue;

            DataGridColumn? column = null;

            var typeName = field.Type; // e.g. "System.Int32"

            if (string.Equals(typeName, "System.Boolean", StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(typeName, "System.DateTime", StringComparison.OrdinalIgnoreCase))
            {
                column = new DataGridTemplateColumn
                {
                    Header = field.Alias,
                    CellTemplate = CreateDateTemplate($"Attributes[{key}]"),
                    CellEditingTemplate = CreateDateTemplate($"Attributes[{key}]")
                };
            }
            else if (typeName.StartsWith("System.Int", StringComparison.OrdinalIgnoreCase) ||
                     typeName.StartsWith("System.Decimal", StringComparison.OrdinalIgnoreCase) ||
                     typeName.StartsWith("System.Double", StringComparison.OrdinalIgnoreCase) ||
                     typeName.StartsWith("System.Single", StringComparison.OrdinalIgnoreCase))
            {
                column = new DataGridTextColumn
                {
                    Header = field.Alias,
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                    },
                    EditingElementStyle = new Style(typeof(TextBox))
                    {
                        Setters = { new Setter(TextBox.TextAlignmentProperty, TextAlignment.Right) }
                    }
                };
            }
            else if (string.Equals(typeName, "System.String", StringComparison.OrdinalIgnoreCase))
            {
                column = new DataGridTextColumn
                {
                    Header = field.Alias,
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters =
            {
                new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap),
                new Setter(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight)
            },
                        Triggers =
            {
                new DataTrigger
                {
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Converter = new RtlFlowDirectionConverter()
                    },
                    Value = FlowDirection.RightToLeft,
                    Setters =
                    {
                        new Setter(FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft)
                    }
                }
            }
                    },
                    EditingElementStyle = new Style(typeof(TextBox))
                    {
                        Setters =
            {
                new Setter(FrameworkElement.FlowDirectionProperty, FlowDirection.LeftToRight)
            },
                        Triggers =
            {
                new DataTrigger
                {
                    Binding = new Binding($"Attributes[{key}]")
                    {
                        Converter = new RtlFlowDirectionConverter()
                    },
                    Value = FlowDirection.RightToLeft,
                    Setters =
                    {
                        new Setter(FrameworkElement.FlowDirectionProperty, FlowDirection.RightToLeft)
                    }
                }
            }
                    }
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

            grid.Columns.Add(column);            
        } 
    }

    // Helper for DatePicker template
    private static DataTemplate CreateDateTemplate(string bindingPath)
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
}
