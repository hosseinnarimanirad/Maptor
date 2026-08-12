using System;
using System.Collections.ObjectModel;

using IRI.Maptor.Jab.Wpf.Helpers;
using IRI.Maptor.Jab.Core;
using IRI.Maptor.Jab.Core.Models;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Themes;

public class ThemeSelectionViewModel : Notifier
{
    private readonly MahAppsThemeColor? _originalTheme;
    private readonly ThemeMode? _originalMode;
    private readonly GeneralSettingsModel _generalSettings;
    private readonly Action<bool> _requestClose;

    private MahAppsThemeColor _selectedTheme;
    private ThemeMode _selectedMode;

    public ThemeSelectionViewModel(GeneralSettingsModel generalSettings, Action<bool> requestClose)
    {
        _originalTheme = generalSettings?.MahAppsTheme;
        _originalMode = generalSettings?.MahAppsThemeMode;
        _generalSettings = generalSettings ?? throw new ArgumentNullException(nameof(generalSettings));
        _requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));

        _selectedMode = generalSettings?.MahAppsThemeMode ?? ThemeMode.Light;

        LoadThemes(generalSettings?.MahAppsTheme);
    }

    /// <summary>
    /// Light or dark. Applied live like the accent, so the whole app previews the change
    /// while the dialog is open; Cancel puts both back.
    /// </summary>
    public ThemeMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode == value)
                return;

            _selectedMode = value;

            // the tiles paint their preview from this, so keep every item in step
            foreach (var theme in AvailableThemes)
                theme.Mode = value;

            _generalSettings.MahAppsThemeMode = value;
            ThemeHelper.ApplyTheme(SelectedTheme, value);

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDarkMode));
        }
    }

    /// <summary>
    /// Two-way friendly view of <see cref="SelectedMode"/> for a toggle switch.
    /// </summary>
    public bool IsDarkMode
    {
        get => SelectedMode == ThemeMode.Dark;
        set => SelectedMode = value ? ThemeMode.Dark : ThemeMode.Light;
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
            theme.Mode = _selectedMode;
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
                        ThemeHelper.ApplyTheme(theme.Color, SelectedMode);
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
                    _generalSettings.MahAppsThemeMode = _originalMode;
                    ThemeHelper.ApplyTheme(_originalTheme, _originalMode);
                    _requestClose(false);
                });
            }
            return _cancelCommand;
        }
    }
}
