using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Events;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Sta.Persistence.Abstractions;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Layers;

public interface ILayer
{
    Guid LayerId { get; }

    int AuxilaryId { get; set; }

    Guid ParentLayerId { get; /*set; */}

    string ParentLayerName { get; /*set;*/ }

    string LayerName { get; set; }

    LayerType Type { get; }

    bool IsMovable { get; set; }

    BoundingBox Extent { get; }

    IDataSource? DataSource { get; }

    RenderMode RenderMode { get; }

    RasterizationMethod RasterizationMethod { get; }

    bool IsGroupLayer { get; set; }

    bool? AllChildsVisible { get; set; }

    ILayer? Parent { get; set; }

    bool IsRootLayer { get; }

    ObservableCollection<ILayer> SubLayers { get; set; }

    int ZIndex { get; set; }

    // is layer discoverable in identify
    bool IsSearchable { get; set; }

    //VisualParameters VisualParameters { get; set; }

    //LabelParameters Labels { get; set; }

    //bool IsValid { get; set; }

    //void Invalidate();

    bool IsSelectedInToc { get; set; }

    bool IsExpandedInToc { get; set; }

    bool ShowInToc { get; set; }

    int TocOrder { get; set; }

    bool CanReorderInToc { get; }

    bool CanUserDelete { get; }

    bool IsInScaleRange { get; set; }

    bool IsNotInScaleRange { get; }

    double Opacity { get; set; }

    Visibility Visibility { get; set; }

    ScaleInterval VisibleRange { get; set; }

    FrameworkElement? Element { get; set; }

    List<ILegendCommand> Commands { get; set; }

    List<IFeatureTableCommand> FeatureTableCommands { get; set; }

    Action<ILayer>? RequestChangeVisibility { get; set; }

    Action<ILayer>? RequestRefreshWhenDataLoaded { get; set; }

    public Func<ILayer, Task> RequestMoveLayerDown { get; set; }
    public Func<ILayer, Task> RequestMoveLayerUp { get; set; }

    RelayCommand ChangeSymbologyCommand { get; }

    event EventHandler<CustomEventArgs<VisualParameters>> OnVisibilityChanged;

    event EventHandler<ILayer> OnLayerInitilized;

    //event EventHandler<CustomEventArgs<VisualParameters>> OnLabelChanged;

    bool CanRenderLayer(double mapScale);

    //bool CanRenderLabels(double mapScale);

    // Data-source / layer status flags used for UI feedback (TOC, legend, etc.)
    bool IsBusy { get; }

    bool IsLoaded { get; }

    bool HasPendingChanges { get; }

    bool IsClientFiltered { get; }

    bool HasError { get; }

    bool LayerNameCanBeChanged { get; }

    bool CanMoveLayerUp { get; set; }
    bool CanMoveLayerDown { get; set; }

    void UpdateAllChildsVisible();
}
