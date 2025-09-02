using IRI.Maptor.Jab.Common.Properties;
using System;
using System.ComponentModel;
using System.Globalization; 
using System.Windows;

namespace IRI.Maptor.Jab.Common.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static LocalizationManager Instance { get; } = new LocalizationManager();

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
}