using System.Windows;
using System.Windows.Controls;
using IRI.Maptor.Jab.Common.ViewModels;

namespace IRI.Maptor.Jab.Controls.Views;

/// <summary>
/// Interaction logic for GoToView.xaml
/// </summary>
public partial class GoToView : UserControl
{
    public GoToViewModel Presenter { get => this.DataContext as GoToViewModel; }

    public GoToView()
    {
        InitializeComponent(); 
    }
}
