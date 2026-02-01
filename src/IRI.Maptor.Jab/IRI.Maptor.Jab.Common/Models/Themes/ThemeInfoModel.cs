using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IRI.Maptor.Jab.Common.Assets.Attributes;

namespace IRI.Maptor.Jab.Common.Models.Themes;

public class ThemeInfoModel : Notifier, IDisposable
{
    public int Id { get; private set; }

    public string ColorName { get => Color.ToString(); }

    public string ThemeName { get => $"{Mode.ToString()}.{ColorName}"; }

    private string DisplayNameResourceKey { get => $"theme_color_{ColorName.ToLower()}"; }

    public string DisplayName => LocalizationManager.Instance[DisplayNameResourceKey];

    //public string DisplayName { get; }
    public string AccentColor { get; }

    public MahAppsThemeColor Color { get; }

    public ThemeMode Mode { get; }


    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            RaisePropertyChanged();
        }
    }

    public ThemeInfoModel(/*int id, */MahAppsThemeColor color, /*string colorName,*/ /*string displayNameResourceKey,*/ /*string accentColor, */ThemeMode mode = ThemeMode.Light)
    {
        Id = (int)color;

        this.Color = color;

        //ColorName = color.ToString();

        //this.DisplayNameResourceKey = displayNameResourceKey;

        AccentColor = color.GetAttribute<ColorAttribute>()?.HexColor ?? string.Empty;

        Mode = mode;

        LocalizationManager.Instance.LanguageChanged += Instance_LanguageChanged;
    }

    private void Instance_LanguageChanged()
    {
        RaisePropertyChanged(nameof(DisplayName));
    }

    public override int GetHashCode()
    {
        return Color.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        var other = obj as ThemeInfoModel;

        if (other is null)
            return false;

        return other.Color == this.Color;
    }

    #region IDispose

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                LocalizationManager.Instance.LanguageChanged -= Instance_LanguageChanged;
            }

            // Dispose unmanaged resources here if any
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}