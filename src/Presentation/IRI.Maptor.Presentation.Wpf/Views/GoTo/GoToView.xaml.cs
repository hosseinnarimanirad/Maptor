using System.Windows.Controls;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for GoToView.xaml
/// </summary>
public partial class GoToView : UserControl
{
    public GoToViewModel? Presenter => this.DataContext as GoToViewModel;

    public GoToView()
    {
        InitializeComponent();
    }

    /// <summary>Puts the caret in the free-text coordinate box.</summary>
    public void FocusQuickEntry()
    {
        quickEntryBox.Focus();
        Keyboard.Focus(quickEntryBox);
    }
}
