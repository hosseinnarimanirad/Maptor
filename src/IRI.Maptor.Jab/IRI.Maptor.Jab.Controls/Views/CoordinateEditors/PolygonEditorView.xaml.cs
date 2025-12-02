using IRI.Maptor.Jab.Common.ViewModels.CoordinateEditor;
using System.Linq;
using System.Windows;
using System.Windows.Controls; 

namespace IRI.Maptor.Jab.Controls.Views.General.CoordinateEditors;

/// <summary>
/// Interaction logic for PolygonEditorView.xaml
/// </summary>
public partial class PolygonEditorView : UserControl
{
    public PolygonEditorView()
    {
        InitializeComponent();
        DataContextChanged += PolygonEditorView_DataContextChanged;
    }

    private void PolygonEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is PolygonEditorViewModel presenter)
        {
            if (presenter.ExteriorRing != null)
            {
                //var exteriorPresenter = new LineStringEditorPresenter(presenter.ExteriorRing.Points);
                //var exteriorEditor = this.FindName("ExteriorRingEditor") as LineStringEditorView;
                //if (exteriorEditor != null)
                //{
                //    exteriorEditor.DataContext = exteriorPresenter;
                //}
            }
        }
    }
}

