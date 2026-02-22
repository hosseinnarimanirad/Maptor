using System.Windows;
using System.Windows.Data;
using MahApps.Metro.Controls;
using IRI.Maptor.Jab.Common.Localization;

namespace IRI.Maptor.Jab.Controls.Views.Dialogs;

public partial class FeatureChangesDialogView : MetroWindow
{
    public FeatureChangesDialogView()
    {
        InitializeComponent();

        var loc = LocalizationManager.Instance;

        BindingOperations.SetBinding(this, TitleProperty, new Binding("[featureChanges_title]") { Source = loc });

        BindingOperations.SetBinding(this, FlowDirectionProperty, new Binding("CurrentFlowDirection") { Source = loc });
    }
}
