using System.Windows;
using MahApps.Metro.Controls;  

namespace IRI.Maptor.Jab.Controls.Views.Dialogs;

/// <summary>
/// Interaction logic for ThemeSelectionDialog.xaml
/// </summary>
public partial class ThemeSelectionDialog : MetroWindow
{
    //private ApplicationPresenter? _presenter;

    //public ApplicationPresenter? Presenter
    //{
    //    get => _presenter;
    //    set
    //    {
    //        _presenter = value;
    //        if (_presenter != null)
    //        {
    //            var currentTheme = _presenter.GeneralSettings?.MahAppsTheme ?? MahAppsThemeColor.Amber;
    //            DataContext = new ThemeSelectionViewModel(
    //                currentTheme,
    //                _presenter.GeneralSettings!,
    //                result =>
    //                {
    //                    DialogResult = result;
    //                    Close();
    //                });
    //        }
    //    }
    //}

    public ThemeSelectionDialog()
    {
        InitializeComponent();
    }
}
