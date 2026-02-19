using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Models.Map;
using IRI.Maptor.Sta.Spatial.Primitives;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;
using IRI.Maptor.Jab.Controls.Common.Behaviors;
using IRI.Maptor.Sta.Common.Enums;
using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for RadFeatureTable.xaml
/// </summary>
public partial class FeatureTable : UserControl
{
    //private Feature<Point>? _pendingEditFeature;
    private Dictionary<string, object>? _pendingAttributes;
    private bool _editingFeature = false;

    public SelectedLayer Presenter { get { return (this.DataContext as SelectedLayer)!; } }



    public bool IsZoomToGeometryEnabled
    {
        get { return (bool)GetValue(IsZoomToGeometryEnabledProperty); }
        set { SetValue(IsZoomToGeometryEnabledProperty, value); }
    }
    public static readonly DependencyProperty IsZoomToGeometryEnabledProperty =
      DependencyProperty.Register(nameof(IsZoomToGeometryEnabled), typeof(bool), typeof(FeatureTable), new PropertyMetadata(false));


    public bool CanUserEditGeometry
    {
        get { return (bool)GetValue(CanUserEditGeometryProperty); }
        set { SetValue(CanUserEditGeometryProperty, value); }
    }
    public static readonly DependencyProperty CanUserEditGeometryProperty =
        DependencyProperty.Register(nameof(CanUserEditGeometry), typeof(bool), typeof(FeatureTable), new PropertyMetadata(false));


    public bool CanUserEditAttribute
    {
        get { return (bool)GetValue(CanUserEditAttributeProperty); }
        set { SetValue(CanUserEditAttributeProperty, value); }
    }
    public static readonly DependencyProperty CanUserEditAttributeProperty =
        DependencyProperty.Register(nameof(CanUserEditAttribute), typeof(bool), typeof(FeatureTable), new PropertyMetadata(false));



    public FeatureTable()
    {
        InitializeComponent();
    }

    private void grid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        _editingFeature = true;

        if (e.Row?.Item is Feature<Point> feature)
            _pendingAttributes = DictionaryHelper.Copy(feature.Attributes);// new Feature<Point>(feature.TheGeometry?.Clone(), attrsCopy) { Id = feature.Id };

        else
            _pendingAttributes = null;
    }

    private void grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        try
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row?.Item as Feature<Point>;

                if (item is null || _pendingAttributes is null)
                    return;

                if (DictionaryHelper.AreAttributesEqual(_pendingAttributes, item.Attributes))
                    return;

                Presenter?.UpdateAttributes(item, _pendingAttributes);
            }
        }
        finally
        {
            _editingFeature = false;
            _pendingAttributes = null;
        }
    }


    private void grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_editingFeature)
            return;

        if (IsZoomToGeometryEnabled)
        {
            var selectedItems = grid.SelectedItems.Cast<Feature<Point>>();

            var action = new Action(() =>
            {
                if (selectedItems?.Count() == 1 && selectedItems.First().TheGeometry.Type == GeometryType.Point)
                {
                    Presenter?.RequestFlashSinglePoint?.Invoke(selectedItems.First());
                }
            });

            Presenter?.RequestZoomTo?.Invoke(selectedItems, action);
        }
    }

    private void grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = grid.SelectedItems?.Count > 0
            ? grid.SelectedItems.Cast<Feature<Point>>()
            : Enumerable.Empty<Feature<Point>>();

        this.Presenter?.UpdateHighlightedFeatures(selected);
    }

    private void grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => DataGridDictionaryBehavior.Regenerate(sender);


    // to enable off-column row selection
    private void grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (grid.ItemsSource == null)
            return;

        var pt = e.GetPosition(grid);

        var hitResult = VisualTreeHelper.HitTest(grid, pt);

        if (hitResult?.VisualHit is not DependencyObject hit)
            return;

        var cell = FindVisualParent<DataGridCell>(hit);

        if (cell != null)
            return;

        var row = FindVisualParent<DataGridRow>(hit);

        if (row == null)
            return;

        if (row.Item == null)
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            if (grid.SelectedItems.Contains(row.Item))
                grid.SelectedItems.Remove(row.Item);

            else
                grid.SelectedItems.Add(row.Item);
        }
        else
        {
            grid.SelectedItem = row.Item;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent)
                return parent;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    #region Old codes

    //private void grid_RowEditEnded(object sender, Telerik.Windows.Controls.GridViewRowEditEndedEventArgs e)
    //{
    //    if (e.EditAction == Telerik.Windows.Controls.GridView.GridViewEditAction.Commit)
    //    {
    //        if (e.EditOperationType == Telerik.Windows.Controls.GridView.GridViewEditOperationType.Edit)
    //        {
    //            var item = e.EditedItem as Feature<Point>;

    //            Presenter.UpdateFeature(item);
    //        }
    //        else if (e.EditOperationType == Telerik.Windows.Controls.GridView.GridViewEditOperationType.Insert)
    //        {

    //        }
    //        else
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }
    //}

    //private void RadGridView_AutoGeneratingColumn(object sender, Telerik.Windows.Controls.GridViewAutoGeneratingColumnEventArgs e)
    //{
    //    if (Type.GetTypeCode(e.ItemPropertyInfo.PropertyType) == TypeCode.Object)
    //    {
    //        e.Cancel = true;
    //    }

    //    switch (Type.GetTypeCode(e.ItemPropertyInfo.PropertyType))
    //    {
    //        case TypeCode.Double:
    //        case TypeCode.Int16:
    //        case TypeCode.Int32:
    //        case TypeCode.Int64:
    //        case TypeCode.Decimal:
    //        case TypeCode.SByte:
    //        case TypeCode.Single:
    //            e.Column.TextAlignment = TextAlignment.Left;
    //            break;
    //        default:
    //            break;
    //    }

    //    if (e.Column.Header.ToString().EqualsIgnoreCase(nameof(Feature<Point>.TheGeometry)) ||
    //        e.Column.Header.ToString().EqualsIgnoreCase("TheGeometry"/*nameof(IGeometryAware.TheGeometry)*/))
    //    {
    //        e.Cancel = true;
    //    }

    //    if (Presenter.Fields.IsNullOrEmpty())
    //        return;

    //    var field = Presenter?.Fields?.FirstOrDefault(f => f.Name == e.Column.Header.ToString());

    //    if (field is not null)
    //        e.Column.Header = field.Alias;
    //}

    //private void grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    //{
    //    if (Type.GetTypeCode(e.PropertyType) == TypeCode.Object)
    //    {
    //        e.Cancel = true;
    //    }

    //    switch (Type.GetTypeCode(e.PropertyType))
    //    {
    //        case TypeCode.Double:
    //        case TypeCode.Int16:
    //        case TypeCode.Int32:
    //        case TypeCode.Int64:
    //        case TypeCode.Decimal:
    //        case TypeCode.SByte:
    //        case TypeCode.Single:
    //            //e.Column.TextAlignment = TextAlignment.Left;
    //            break;
    //        default:
    //            break;
    //    }

    //    if (e.Column.Header.ToString().EqualsIgnoreCase(nameof(Feature<Point>.TheGeometry)) ||
    //        e.Column.Header.ToString().EqualsIgnoreCase("TheGeometry"/*nameof(IGeometryAware.TheGeometry)*/))
    //    {
    //        e.Cancel = true;
    //    }

    //    if (Presenter.Fields.IsNullOrEmpty())
    //        return;

    //    var field = Presenter?.Fields?.FirstOrDefault(f => f.Name == e.Column.Header.ToString());

    //    if (field is not null)
    //        e.Column.Header = field.Alias;
    //}



    //private void grid_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
    //{
    //    this.Presenter.UpdateHighlightedFeatures(grid.SelectedItems.Cast<Feature<Point>>());
    //}


    #endregion
}
