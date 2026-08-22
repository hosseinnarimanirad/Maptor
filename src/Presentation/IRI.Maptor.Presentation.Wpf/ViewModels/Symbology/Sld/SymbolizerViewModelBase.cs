using System;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Core.Ogc.SLD;

namespace IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

/// <summary>
/// Base ViewModel for all symbolizer types
/// </summary>
public abstract class SymbolizerViewModelBase : Notifier
{
    private string _geometryPropertyName;

    public string GeometryPropertyName
    {
        get => _geometryPropertyName;
        set
        {
            _geometryPropertyName = value;
            RaisePropertyChanged();
        }
    }

    public abstract Symbolizer ToSymbolizer();

    public abstract void FromSymbolizer(Symbolizer symbolizer);

    public abstract string SymbolizerType { get; }
}

