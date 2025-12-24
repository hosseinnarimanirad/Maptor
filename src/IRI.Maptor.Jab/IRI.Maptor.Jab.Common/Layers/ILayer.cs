using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Common.Models.Legend;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Events;

namespace IRI.Maptor.Jab.Common;

public interface ILayer
{
    Guid LayerId { get; }

    int AuxilaryId { get; set; }

    Guid ParentLayerId { get; set; }

    string ParentLayerName { get; set; }

    string LayerName { get; set; }

    LayerType Type { get; }

    BoundingBox Extent { get; }

    RenderMode RenderMode { get; }

    RasterizationMethod RasterizationMethod { get; }

    bool IsGroupLayer { get; set; }

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

    bool CanUserDelete { get; }

    bool IsInScaleRange { get; set; }

    double Opacity { get; set; }

    Visibility Visibility { get; set; }
    
    ScaleInterval VisibleRange { get; set; }

    FrameworkElement? Element { get; set; }

    List<ILegendCommand> Commands { get; set; }

    List<IFeatureTableCommand> FeatureTableCommands { get; set; }

    Action<ILayer>? RequestChangeVisibility { get; set; }

    RelayCommand ChangeSymbologyCommand { get; }

    event EventHandler<CustomEventArgs<VisualParameters>> OnVisibilityChanged;

    //event EventHandler<CustomEventArgs<VisualParameters>> OnLabelChanged;

    bool CanRenderLayer(double mapScale);

    //bool CanRenderLabels(double mapScale);
}
