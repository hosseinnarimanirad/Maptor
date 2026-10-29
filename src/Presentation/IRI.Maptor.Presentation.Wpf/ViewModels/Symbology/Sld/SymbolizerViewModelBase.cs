using System;
using IRI.Maptor.Presentation.Core;
using IRI.Maptor.Presentation.Core.Localization;
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

    private bool _isExpanded = true;

    /// <summary>Editor-only state: whether the symbolizer's card shows its fields. Not part of the SLD.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>The symbolizer kind in the user's language ("Fill", "Line", "Label", …).</summary>
    public string DisplayName => LocalizationManager.Instance[$"sldEditor_symbolizer_{SymbolizerType.ToLowerInvariant()}"];

    public abstract Symbolizer ToSymbolizer();

    public abstract void FromSymbolizer(Symbolizer symbolizer);

    public abstract string SymbolizerType { get; }
}
