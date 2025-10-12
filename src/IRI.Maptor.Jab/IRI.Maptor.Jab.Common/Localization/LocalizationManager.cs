using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Properties;

namespace IRI.Maptor.Jab.Common.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly LocalizationManager _instance = new LocalizationManager();
    public static LocalizationManager Instance => _instance;

    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    // Custom event to avoid PropertyChanged overhead
    public event Action LanguageChanged;

    public event Action FlowDirectionChanged;

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                CultureInfo.CurrentUICulture = value;
                CultureInfo.CurrentCulture = value;
                LanguageChanged?.Invoke();

                FlowDirectionChanged?.Invoke(); // New event

                Resources.Culture = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); // refresh all bindings
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFlowDirection)));
            }
        }
    }

    public FlowDirection CurrentFlowDirection => CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    //public string this[string key] => Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

    public string this[string key]
    {
        get
        {
            // Explicitly specify to fallback to default resources
            return Resources.ResourceManager.GetString(key, CurrentCulture)
                   ?? Resources.ResourceManager.GetString(key, CultureInfo.InvariantCulture)
                   ?? $"#{key}"; // Fallback for missing keys

            //// Explicitly use the current culture and prevent caching issues
            //var resourceSet = Resources.ResourceManager.GetResourceSet(
            //    CurrentCulture,
            //    true,  // load if not found
            //    false); // don't use cached resources
            //return resourceSet?.GetString(key) ?? $"#{key}#";
        }
    }

    public bool IsPersian => CurrentCulture.Name.Equals("fa-IR", StringComparison.OrdinalIgnoreCase);

    public LocalizationManager()
    {
        CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }


    //public bool IsFrench => CurrentCulture.Name.Equals("fr-FR", StringComparison.OrdinalIgnoreCase);

    public void SetCulture(CultureInfo culture)
    {
        CurrentCulture = culture;

    }

    public string GetDefaultValue(string key)
    {
        return Resources.ResourceManager.GetString(key, CultureInfo.InvariantCulture)
               ?? $"#{key}#";
    }

    public static string GetLocalizedNumberString(object value)
    { 
        // Use culture from binding if available, otherwise fallback to current thread culture
      
        //culture ??= CultureInfo.CurrentUICulture;
        var culture = Instance.CurrentCulture;

        var isPersian = string.Equals(culture.Name, "fa-IR", StringComparison.OrdinalIgnoreCase);

        if (value is IFormattable formattable)
        { 
            if (isPersian)
            {
                return formattable?.ToString()?.LatinNumbersToFarsiNumbers() ?? string.Empty;
            }
            else
            {
                return formattable.ToString(null, culture);
            }
        }
        else if (value is string str && isPersian)
        {
            return str?.LatinNumbersToFarsiNumbers() ?? string.Empty;
        }

        return value?.ToString() ?? string.Empty;
    }
}