using IRI.Maptor.Jab.Wpf;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IRI.Maptor.Jab.Controls.Dialogs;

/// <summary>
/// Interaction logic for LayerSettingsDialogView.xaml
/// </summary>
public partial class LayerSettingsDialogView : MetroWindow
{
    public LayerSettingsDialogView()
    {
        InitializeComponent();
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
