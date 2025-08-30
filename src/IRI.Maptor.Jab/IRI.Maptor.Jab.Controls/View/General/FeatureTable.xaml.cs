using IRI.Maptor.Jab.Common.Models.Map;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Point = IRI.Maptor.Sta.Common.Primitives.Point;
using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls.View;

/// <summary>
/// Interaction logic for RadFeatureTable.xaml
/// </summary>
public partial class FeatureTable : UserControl
{
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

    // Using a DependencyProperty as the backing store for CanUserEditGeometry.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CanUserEditGeometryProperty =
        DependencyProperty.Register(nameof(CanUserEditGeometry), typeof(bool), typeof(FeatureTable), new PropertyMetadata(false));



    public bool CanUserEditAttribute
    {
        get { return (bool)GetValue(CanUserEditAttributeProperty); }
        set { SetValue(CanUserEditAttributeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CanUserEditAttribute.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CanUserEditAttributeProperty =
        DependencyProperty.Register(nameof(CanUserEditAttribute), typeof(bool), typeof(FeatureTable), new PropertyMetadata(false));


    public SelectedLayer Presenter { get { return this.DataContext as SelectedLayer; } }

    public FeatureTable()
    {
        InitializeComponent();
    }

    


    private void grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (IsZoomToGeometryEnabled)
        {
            var selectedItems = grid.SelectedItems.Cast<Feature<Point>>();

            var action = new Action(() =>
            {
                if (selectedItems?.Count() == 1 && selectedItems.First().TheGeometry.Type == GeometryType.Point)
                {
                    Presenter.RequestFlashSinglePoint(selectedItems.First());
                }
            });

            Presenter.RequestZoomTo?.Invoke(selectedItems, action);
        }
    }

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

    private void grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            //if (e.EditOperationType == Telerik.Windows.Controls.GridView.GridViewEditOperationType.Edit)
            //{
            var item = e.EditingElement.DataContext as Feature<Point>;

            Presenter.UpdateFeature(item);
            //}
            //else if (e.EditOperationType == Telerik.Windows.Controls.GridView.GridViewEditOperationType.Insert)
            //{

            //}
            //else
            //{
            //    throw new NotImplementedException();
            //}
        }
    }

    private void grid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {

    }

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

    private void grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (Type.GetTypeCode(e.PropertyType) == TypeCode.Object)
        {
            e.Cancel = true;
        }

        switch (Type.GetTypeCode(e.PropertyType))
        {
            case TypeCode.Double:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.SByte:
            case TypeCode.Single:
                //e.Column.TextAlignment = TextAlignment.Left;
                break;
            default:
                break;
        }

        if (e.Column.Header.ToString().EqualsIgnoreCase(nameof(Feature<Point>.TheGeometry)) ||
            e.Column.Header.ToString().EqualsIgnoreCase("TheGeometry"/*nameof(IGeometryAware.TheGeometry)*/))
        {
            e.Cancel = true;
        }

        if (Presenter.Fields.IsNullOrEmpty())
            return;

        var field = Presenter?.Fields?.FirstOrDefault(f => f.Name == e.Column.Header.ToString());

        if (field is not null)
            e.Column.Header = field.Alias;
    }



    //private void grid_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
    //{
    //    this.Presenter.UpdateHighlightedFeatures(grid.SelectedItems.Cast<Feature<Point>>());
    //}

    private void grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        this.Presenter.UpdateHighlightedFeatures(grid.SelectedItems.Cast<Feature<Point>>());
    }
}
