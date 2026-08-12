using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Primitives;

using Point = IRI.Maptor.Sta.Common.Primitives.Point;
using IRI.Maptor.Sta.Common.Enums;
using System.Collections.Generic;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Jab.Wpf.Behaviors;
using IRI.Maptor.Jab.Wpf.Models;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IRI.Maptor.Jab.Controls;

/// <summary>
/// Interaction logic for RadFeatureTable.xaml
/// </summary>
public partial class FeatureTable : UserControl
{
    //private Feature<Point>? _pendingEditFeature;
    private Dictionary<string, object>? _pendingAttributes;

    private bool _editingFeature = false;

    // true while this control is applying the view-model selection to the grid
    private bool _syncingSelection = false;

    // true while a grid selection change is being pushed into the view-model
    private bool _updatingHighlightFromGrid = false;

    private SelectedLayer? _currentLayer;

    private INotifyCollectionChanged? _currentFeaturesCollection;

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
            var item = e.Row?.Item as Feature<Point>;

            if (item is null || _pendingAttributes is null)
                return;

            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (DictionaryHelper.AreAttributesEqual(_pendingAttributes, item.Attributes))
                    return;

                Presenter?.UpdateAttributes(item, _pendingAttributes);
            }
            else
            {
                // the column bindings use UpdateSourceTrigger=PropertyChanged, so the
                // dictionary already holds the typed value; put the old values back
                foreach (var kvp in _pendingAttributes)
                    item.Attributes[kvp.Key] = kvp.Value;

                Presenter?.RefreshFeatureInView(item);
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
                if (selectedItems?.Count() == 1 && selectedItems.First().GeometryType/*TheGeometry.Type*/ == GeometryType.Point)
                {
                    Presenter?.RequestFlashSinglePoint?.Invoke(selectedItems.First());
                }
            });

            Presenter?.RequestZoomTo?.Invoke(selectedItems, action);
        }
    }

    private void grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection)
            return;

        var selected = grid.SelectedItems?.Count > 0
            ? grid.SelectedItems.Cast<Feature<Point>>().ToList()
            : Enumerable.Empty<Feature<Point>>();

        _updatingHighlightFromGrid = true;

        try
        {
            this.Presenter?.UpdateHighlightedFeatures(selected);
        }
        finally
        {
            _updatingHighlightFromGrid = false;
        }
    }

    //private void grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => DataGridDictionaryBehavior.Regenerate(sender);
     
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



    private void grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DataGridDictionaryBehavior.Regenerate(sender);
        UnsubscribeFromLayerEvents();
        _currentLayer = DataContext as SelectedLayer;
        SubscribeToLayerEvents(_currentLayer);
    }

    #region Scroll to new row added

    
    private void UnsubscribeFromLayerEvents()
    {
        if (_currentLayer is INotifyPropertyChanged inpc)
            inpc.PropertyChanged -= Layer_PropertyChanged;

        if (_currentLayer != null)
            _currentLayer.RequestClearRowSelection = null;

        UnsubscribeFromFeaturesCollection();
        _currentLayer = null;
    }

    private void SubscribeToLayerEvents(SelectedLayer? layer)
    {
        if (layer == null) return;

        // Listen for replacement of the entire Features collection
        if (layer is INotifyPropertyChanged inpc)
            inpc.PropertyChanged += Layer_PropertyChanged;

        layer.RequestClearRowSelection = () => grid.UnselectAll();

        // Subscribe to the current Features collection
        SubscribeToFeaturesCollection(layer.Features);
    }

    private void UnsubscribeFromFeaturesCollection()
    {
        if (_currentFeaturesCollection != null)
            _currentFeaturesCollection.CollectionChanged -= Features_CollectionChanged;
        _currentFeaturesCollection = null;
    }

    private void SubscribeToFeaturesCollection(ObservableCollection<Feature<Point>>? collection)
    {
        UnsubscribeFromFeaturesCollection();

        if (collection == null) return;

        _currentFeaturesCollection = collection;
        collection.CollectionChanged += Features_CollectionChanged;
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedLayer.Features) && _currentLayer != null)
        {
            // The entire collection was replaced – re‑subscribe
            SubscribeToFeaturesCollection(_currentLayer.Features);
        }
        else if (e.PropertyName == nameof(SelectedLayer.HighlightedFeatures) && !_updatingHighlightFromGrid)
        {
            // Selection changed on the view-model side (page refresh restore,
            // map identify, ...) – mirror it onto the grid
            ApplyVmSelectionToGrid();
        }
    }

    private void ApplyVmSelectionToGrid()
    {
        if (_currentLayer?.HighlightedFeatures is null)
            return;

        _syncingSelection = true;

        try
        {
            grid.SelectedItems.Clear();

            foreach (var feature in _currentLayer.HighlightedFeatures)
            {
                if (grid.Items.Contains(feature))
                    grid.SelectedItems.Add(feature);
            }
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void Features_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Find the first newly added feature that is marked as "New"
            var newFeature = e.NewItems.Cast<Feature<Point>>()
                               .FirstOrDefault(f => f.Status == FeatureStatus.New);

            if (newFeature != null)
            {
                // Scroll after the UI has updated the items control
                grid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    grid.ScrollIntoView(newFeature);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }

    #endregion
}
