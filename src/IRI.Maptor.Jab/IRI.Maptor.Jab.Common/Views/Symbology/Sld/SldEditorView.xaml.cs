using System.Windows.Controls;
using IRI.Maptor.Jab.Common.ViewModels.Symbology;

namespace IRI.Maptor.Jab.Controls.Symbology.Sld;

/// <summary>
/// Interaction logic for SldEditorView.xaml
/// </summary>
public partial class SldEditorView : UserControl
{
    public SldEditorView()
    {
        InitializeComponent();
    }

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        // TabControl.SelectionChanged bubbles from inner selectors (e.g. list boxes);
        // only react to the tab control's own selection change.
        if (!ReferenceEquals(e.Source, RuleTabControl))
            return;

        if (ReferenceEquals(RuleTabControl.SelectedItem, XmlPreviewTab)
            && DataContext is SldEditorViewModel viewModel)
        {
            viewModel.RefreshPreview();
        }
    }
}
