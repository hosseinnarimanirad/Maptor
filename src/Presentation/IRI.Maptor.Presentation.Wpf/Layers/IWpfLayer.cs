using System;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using IRI.Maptor.Presentation.Wpf.Models;
using IRI.Maptor.Presentation.Wpf.Models.Legend;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Layers;

namespace IRI.Maptor.Presentation.Wpf.Layers;

/// <summary>
/// WPF-specific extension of ILayer. Adds WPF types (Visibility, FrameworkElement, ICollectionView)
/// that cannot be used in the platform-neutral Jab.Core.
/// All WPF layer implementations should implement this interface.
/// </summary>
public interface IWpfLayer : ILayer
{
    /// <summary>
    /// WPF visibility; kept under the original name for backward compatibility.
    /// Syncs with the platform-neutral IsVisible property on ILayer.
    /// </summary>
    Visibility Visibility { get; set; }

    FrameworkElement? Element { get; set; }

    Func<ILayer, System.ComponentModel.ICollectionView?, Task>? RequestMoveLayerDown { get; set; }

    Func<ILayer, System.ComponentModel.ICollectionView?, Task>? RequestMoveLayerUp { get; set; }

    RelayCommand ChangeSymbologyCommand { get; }

    List<ILegendCommand> Commands { get; set; }

    List<IFeatureTableCommand> FeatureTableCommands { get; set; }

    event EventHandler<CustomEventArgs<VisualParameters>> OnVisibilityChanged;
}
