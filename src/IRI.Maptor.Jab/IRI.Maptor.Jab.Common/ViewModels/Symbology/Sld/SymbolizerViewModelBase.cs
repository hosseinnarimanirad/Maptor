using System;
using IRI.Maptor.Jab.Common;
using IRI.Maptor.Sta.Ogc.SLD;

namespace IRI.Maptor.Jab.Common.ViewModels.Symbology.Sld;

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

