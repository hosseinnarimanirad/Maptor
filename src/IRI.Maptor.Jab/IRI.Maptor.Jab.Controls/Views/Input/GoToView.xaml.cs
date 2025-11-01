using System.Windows;
using System.Windows.Controls;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for GoToView.xaml
/// </summary>
public partial class GoToView : UserControl
{
    public Presenters.GoToPresenter Presenter { get => this.DataContext as Presenters.GoToPresenter; }

    public GoToView()
    {
        InitializeComponent(); 
    }
}
