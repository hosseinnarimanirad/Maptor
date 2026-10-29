using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Controls.Symbology.Sld;

/// <summary>
/// The SLD editor's three panes (rules, rule editor, preview) and its bottom panels. All state lives in
/// <see cref="ViewModels.Symbology.SldEditorViewModel"/>; the XML panel refreshes itself while open.
/// </summary>
public partial class SldEditorView : UserControl
{
    public SldEditorView()
    {
        InitializeComponent();
    }

    /// <summary>Puts the cursor in the selected rule's legend text (the window's F2).</summary>
    public void FocusRuleTitle()
    {
        if (ruleTitleBox.IsVisible)
        {
            ruleTitleBox.Focus();
            ruleTitleBox.SelectAll();
        }
    }
}
