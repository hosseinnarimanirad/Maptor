using System.Windows.Controls;
using System.Windows.Input;

using IRI.Maptor.Presentation.Wpf.ViewModels;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>
/// Interaction logic for MgrsGoToView.xaml
/// </summary>
public partial class MgrsGoToView : UserControl
{
    public MgrsGoToViewModel? Presenter => this.DataContext as MgrsGoToViewModel;

    public MgrsGoToView()
    {
        InitializeComponent();
    }

    /// <summary>Puts the caret in the reference box.</summary>
    public void FocusReference()
    {
        referenceBox.Focus();
        Keyboard.Focus(referenceBox);
    }
}
