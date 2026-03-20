using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
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

    private readonly List<ResourceManager> _registeredManagers = new();

    private readonly object _managersLock = new();

    // Custom event to avoid PropertyChanged overhead
    public event Action LanguageChanged;

    public event Action FlowDirectionChanged;

    /// <summary>
    /// Registers a ResourceManager for localization lookup. Registered managers are tried first (in registration order),
    /// then Jab.Common Resources (shared across apps).
    /// </summary>
    public void RegisterResourceManager(ResourceManager manager)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        lock (_managersLock)
        {
            if (!_registeredManagers.Contains(manager))
                _registeredManagers.Add(manager);
        }
    }

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

    public string this[string key]
    {
        get
        {
            // Try each manager in order: registered (app-specific) → Resources (Jab.Common, shared)
            foreach (var manager in GetManagersInOrder())
            {
                var value = manager.GetString(key, CurrentCulture)
                            ?? manager.GetString(key, CultureInfo.InvariantCulture);
                if (value != null)
                    return value;
            }
            return $"#{key}";
        }
    }

    private IEnumerable<ResourceManager> GetManagersInOrder()
    {
        lock (_managersLock)
        {
            foreach (var m in _registeredManagers)
                yield return m;
        }
        yield return Resources.ResourceManager;
    }

    public bool IsPersian => CurrentCulture.Name.Equals("fa-IR", StringComparison.OrdinalIgnoreCase);

    public LocalizationManager()
    {
        CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    public void SetCulture(CultureInfo culture)
    {
        CurrentCulture = culture;
    }

    public string GetDefaultValue(string key)
    {
        foreach (var manager in GetManagersInOrder())
        {
            var value = manager.GetString(key, CultureInfo.InvariantCulture);
            if (value != null)
                return value;
        }
        return $"#{key}#";
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