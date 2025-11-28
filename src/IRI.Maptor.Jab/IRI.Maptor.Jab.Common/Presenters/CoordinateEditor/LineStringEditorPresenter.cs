using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Models;
using IRI.Maptor.Jab.Common.Models.CoordinateEditor; 
 
namespace IRI.Maptor.Jab.Common.Presenters.CoordinateEditor;

public class LineStringEditorPresenter : GeometryEditorViewModel
{
     
    public LineStringEditorPresenter(EditableFeatureLayer editableFeatureLayer)
    {
        this.FeatureLayer = editableFeatureLayer;

        //Points = points ?? new ObservableCollection<NotifiablePoint>();
        //Points.CollectionChanged += Points_CollectionChanged;
        
        foreach (var point in Points)
        {
            point.PropertyChanged += Point_PropertyChanged;
        }
        
        UpdateValidationState();

        // Initialize Parts collection for multi-line string support
        //Parts = new ObservableCollection<LineStringEditorPresenter>();
        //Parts.CollectionChanged += Parts_CollectionChanged;
    }
}




