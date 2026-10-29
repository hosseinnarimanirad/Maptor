using System.Windows.Input;
using IRI.Maptor.Presentation.Wpf.ViewModels.Symbology;

namespace IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld;

/// <summary>
/// Interaction logic for SldRuleGeneratorWindow.xaml: the "Generate rules from field values" dialog of the SLD
/// editor. Closes with DialogResult true when rules were generated (<see cref="SldRuleGeneratorViewModel.Result"/>).
/// </summary>
public partial class SldRuleGeneratorWindow : LocalizedMetroWindow
{
    public SldRuleGeneratorWindow(SldRuleGeneratorViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // shown modally: setting the result closes the window
        viewModel.RequestClose = generated => DialogResult = generated;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }
}
