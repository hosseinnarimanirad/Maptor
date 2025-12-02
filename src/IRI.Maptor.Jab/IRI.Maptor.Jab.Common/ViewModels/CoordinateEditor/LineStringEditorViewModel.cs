using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor;

namespace IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;

public class LineStringEditorViewModel : GeometryEditorViewModelBase
{

    public LineStringEditorViewModel(EditableFeatureLayer editableFeatureLayer)
    {
        this.FeatureLayer = editableFeatureLayer;

        // Initialize CurrentPartIndex to 0 (first part)
        // This will trigger RefreshPointsFromCurrentPart() which initializes Points collection
        // Setting it explicitly ensures initialization happens
        CurrentPartIndex = 0;

        this.IsEditable = true;

        // Points are already initialized by RefreshPointsFromCurrentPart() called in CurrentPartIndex setter
        // which also subscribes to PropertyChanged events
        // UpdateValidationState is called by RefreshPointsFromCurrentPart via UpdatePagingProperties
        UpdateValidationState();
    }
}




