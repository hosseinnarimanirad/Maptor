using System.Windows;

using MahApps.Metro.Controls;

namespace IRI.Maptor.Presentation.Wpf.Controls.Dialogs;

/// <summary>Shows a server-generated password once, with a Copy button (GeneratedPasswordDialogViewModel).</summary>
public partial class GeneratedPasswordDialogView : MetroWindow
{
    public GeneratedPasswordDialogView()
    {
        InitializeComponent();

        // the Copy button is the expected next step: start there, so Enter or Space copies
        Loaded += (_, _) => copyButton.Focus();
    }
}
