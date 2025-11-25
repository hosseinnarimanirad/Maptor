using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IRI.Maptor.Jab.Controls.Models.GeometryDetails;
using IRI.Maptor.Jab.Controls.Presenters.CoordinateEditors;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Jab.Controls.Views.General.CoordinateEditors;

/// <summary>
/// Interaction logic for LineStringEditorView.xaml
/// </summary>
public partial class LineStringEditorView : UserControl
{
    public LineStringEditorView()
    {
        InitializeComponent();
    }

    private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is PointInfo pointInfo)
        {
            if (DataContext is LineStringEditorPresenter presenter)
            {
                var point = new IRI.Maptor.Sta.Common.Primitives.Point(pointInfo.X, pointInfo.Y);
                // Trigger zoom through parent
                var parent = this.Parent;
                while (parent != null)
                {
                    if (parent is GeometryDetailsView detailsView)
                    {
                        detailsView.RequestZoomToPoint?.Invoke(point);
                        break;
                    }
                    parent = parent is FrameworkElement fe ? fe.Parent : null;
                }
            }
        }
    }
}

