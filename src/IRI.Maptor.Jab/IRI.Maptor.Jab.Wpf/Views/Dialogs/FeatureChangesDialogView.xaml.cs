using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.Jab.Controls.Dialogs;

public partial class FeatureChangesDialogView : MetroWindow
{
    public FeatureChangesDialogView()
    {
        InitializeComponent();

        var loc = LocalizationManager.Instance;

        BindingOperations.SetBinding(this, TitleProperty, new Binding("[featureChanges_title]") { Source = loc });

        BindingOperations.SetBinding(this, FlowDirectionProperty, new Binding("CurrentFlowDirection") { Source = loc });
    }

    // Close-only footer: no Cancel button to carry IsCancel, so Esc is handled here.
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
