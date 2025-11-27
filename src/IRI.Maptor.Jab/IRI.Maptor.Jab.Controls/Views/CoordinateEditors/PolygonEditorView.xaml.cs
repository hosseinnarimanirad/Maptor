using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Jab.Controls.Presenters.CoordinateEditors;

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
        if (e.NewValue is PolygonEditorPresenter presenter)
        {
            if (presenter.ExteriorRing != null)
            {
                var exteriorPresenter = new LineStringEditorPresenter(presenter.ExteriorRing.Points);
                var exteriorEditor = this.FindName("ExteriorRingEditor") as LineStringEditorView;
                if (exteriorEditor != null)
                {
                    exteriorEditor.DataContext = exteriorPresenter;
                }
            }
        }
    }
}

