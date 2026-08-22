using IRI.Maptor.Extensions;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Presentation.Core.Attributes;

namespace IRI.Maptor.Presentation.Core.Models;

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

    private ThemeMode _mode;

    /// <summary>
    /// Light or dark. Settable so the theme picker can re-render its previews when the
    /// user flips the appearance switch, without rebuilding the whole list.
    /// </summary>
    public ThemeMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
                return;

            _mode = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsDark));
            RaisePropertyChanged(nameof(ThemeName));
        }
    }

    public bool IsDark => Mode == ThemeMode.Dark;


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

        Color = color;
         
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

        return other.Color == Color;
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