using System;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Wpf.Helpers;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Themes;

public class ThemeSelectionViewModel : Notifier
{
    private readonly MahAppsThemeColor? _originalTheme;
    private readonly GeneralSettingsModel _generalSettings;
    private readonly Action<bool> _requestClose;

    private MahAppsThemeColor _selectedTheme;
    private string _selectedThemeDisplayName = "Amber";

    public ThemeSelectionViewModel(GeneralSettingsModel generalSettings, Action<bool> requestClose)
    { 
        _originalTheme = generalSettings?.MahAppsTheme;
        _generalSettings = generalSettings ?? throw new ArgumentNullException(nameof(generalSettings));
        _requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));

        LoadThemes(generalSettings?.MahAppsTheme);
    }

    public ObservableCollection<ThemeInfoModel> AvailableThemes { get; } = new();

    public MahAppsThemeColor SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value)
                return;
            _selectedTheme = value;
            RaisePropertyChanged();
        }
    }
     
    private void LoadThemes(MahAppsThemeColor? currentTheme)
    {
        foreach (var theme in ThemeHelper.AvailableThemes)
        {
            theme.IsSelected = theme.Color == currentTheme;
        }

        AvailableThemes.Clear();
        foreach (var theme in ThemeHelper.AvailableThemes)
        {
            AvailableThemes.Add(theme);
        }

        if (currentTheme is null)
            return;
         
        SelectedTheme = currentTheme.Value; 
    }

    private RelayCommand? _selectThemeCommand;

    public RelayCommand SelectThemeCommand
    {
        get
        {
            if (_selectThemeCommand == null)
            {
                _selectThemeCommand = new RelayCommand(param =>
                {
                    if (param is ThemeInfoModel theme)
                    {
                        foreach (var t in AvailableThemes)
                        {
                            t.IsSelected = t.Color == theme.Color;
                        }

                        SelectedTheme = theme.Color; 
                        _generalSettings.MahAppsTheme = SelectedTheme;
                        ThemeHelper.ApplyTheme(theme.Color);
                    }
                });
            }
            return _selectThemeCommand;
        }
    }

    private RelayCommand? _saveCommand;

    public RelayCommand SaveCommand
    {
        get
        {
            if (_saveCommand == null)
            {
                _saveCommand = new RelayCommand(_ =>
                {
                    //_generalSettings.MahAppsTheme = SelectedTheme;
                    _requestClose(true);
                });
            }
            return _saveCommand;
        }
    }

    private RelayCommand? _cancelCommand;

    public RelayCommand CancelCommand
    {
        get
        {
            if (_cancelCommand == null)
            {
                _cancelCommand = new RelayCommand(_ =>
                {
                    _generalSettings.MahAppsTheme = _originalTheme;
                    ThemeHelper.ApplyTheme(_originalTheme);
                    _requestClose(false);
                });
            }
            return _cancelCommand;
        }
    }
}
