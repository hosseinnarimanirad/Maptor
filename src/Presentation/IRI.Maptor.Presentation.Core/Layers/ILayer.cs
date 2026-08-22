using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Persistence.Abstractions;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Models;

namespace IRI.Maptor.Presentation.Core.Layers;

public interface ILayer : INotifyPropertyChanged
{
    Guid LayerId { get; }

    int AuxilaryId { get; set; }

    Guid ParentLayerId { get; }

    string ParentLayerName { get; }

    string LayerName { get; set; }

    LayerType Type { get; }

    bool IsMovable { get; set; }

    BoundingBox Extent { get; }

    IDataSource? DataSource { get; }

    RenderMode RenderMode { get; }

    RasterizationMethod RasterizationMethod { get; }

    bool IsGroupLayer { get; set; }

    bool? AllChildrenVisible { get; set; }

    ILayer? Parent { get; set; }

    bool IsRootLayer { get; }

    ObservableCollection<ILayer> SubLayers { get; set; }

    int ZIndex { get; set; }

    bool IsSearchable { get; set; }

    #region Toc

    bool IsSelectedInToc { get; set; }

    bool IsExpandedInToc { get; set; }

    bool CanReorderInToc { get; }

    int TocOrder { get; set; }

    string TocGroup { get; set; }

    #endregion

    bool CanUserDelete { get; }

    bool IsInScaleRange { get; set; }

    bool IsNotInScaleRange { get; }

    double Opacity { get; set; }

    /// <summary>
    /// Whether the layer is currently visible. Platform-neutral replacement for WPF Visibility enum.
    /// </summary>
    bool IsVisible { get; set; }

    ScaleInterval VisibleRange { get; set; }

    Action<ILayer>? RequestChangeVisibility { get; set; }

    Action<ILayer>? RequestRefreshWhenDataLoaded { get; set; }

    event EventHandler<ILayer> OnLayerInitialized;

    bool CanRenderLayer(double mapScale);

    bool IsBusy { get; set; }

    bool IsLoaded { get; }

    bool HasPendingChanges { get; }

    bool IsClientFiltered { get; }

    bool HasError { get; }

    bool LayerNameCanBeChanged { get; }

    bool CanMoveLayerUp { get; set; }
    bool CanMoveLayerDown { get; set; }

    void UpdateAllChildrenVisible();
}
