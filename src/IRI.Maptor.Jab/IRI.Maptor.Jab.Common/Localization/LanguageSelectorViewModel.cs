using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace IRI.Maptor.Jab.Common.Localization;

public class LanguageSelectorViewModel : Notifier, IDisposable
{
    private readonly LocalizationManager _localization = LocalizationManager.Instance;
    private readonly Action<CultureInfo>? _onLanguageChanged;
    private bool _disposed;

    /// <summary>
    /// Creates a view model with the default language list.
    /// Includes all languages with Resources.xx.resx: en, fa-IR, ar, fr, es, tr, ru, az, hy, it, ku, pt.
    /// </summary>
    /// <param name="onLanguageChanged">Optional callback when the user selects a different language.</param>
    /// <param name="disabledCultureNames">Optional culture names (e.g. "fr-FR") to show but disable for selection.</param>
    public LanguageSelectorViewModel(
        Action<CultureInfo>? onLanguageChanged = null,
        IEnumerable<string>? disabledCultureNames = null)
    {
        var disabled = disabledCultureNames?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        static bool Enabled(HashSet<string> d, string name) => !d.Contains(name);

        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            // English
            new LanguageItem(new CultureInfo("en-US"), isEnabled: Enabled(disabled, "en-US")),

            // Persian
            new LanguageItem(new CultureInfo("fa-IR"), isEnabled: Enabled(disabled, "fa-IR")),
            
            // Arabic
            new LanguageItem(new CultureInfo("ar-SA"), isEnabled: Enabled(disabled, "ar-SA")),
            
            // Armenian
            new LanguageItem(new CultureInfo("hy-AM"), isEnabled: Enabled(disabled, "hy-AM")),
            
            // Azerbaijani
            new LanguageItem(new CultureInfo("az-Latn-AZ"), isEnabled: Enabled(disabled, "az-Latn-AZ")),

            // French
            new LanguageItem(new CultureInfo("fr-FR"), isEnabled: Enabled(disabled, "fr-FR")),

            // Italian
            new LanguageItem(new CultureInfo("it-IT"), isEnabled: Enabled(disabled, "it-IT")),

            // Kurdish (Sorani script: کوردی) - uses ku-Arab-IQ for Resources.ku-Arab-IQ.resx
            new LanguageItem(GetKurdishCulture(), isEnabled: Enabled(disabled, "ku-IQ"), nativeNameOverride: "کوردی"),

            // Portuguese (Brazil)
            new LanguageItem(new CultureInfo("pt-BR"), isEnabled: Enabled(disabled, "pt-BR")),
            
            // Russian
            new LanguageItem(new CultureInfo("ru-RU"), isEnabled: Enabled(disabled, "ru-RU")),

            // Spanish
            new LanguageItem(new CultureInfo("es-ES"), isEnabled: Enabled(disabled, "es-ES")),

            // Turkish
            new LanguageItem(new CultureInfo("tr-TR"), isEnabled: Enabled(disabled, "tr-TR")),

        };

        _selectedLanguage = GetInitialSelection();
        _onLanguageChanged = onLanguageChanged;
        _localization.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Creates a view model with a custom list of languages.
    /// </summary>
    /// <param name="languages">Custom language items (each can have IsEnabled set via constructor).</param>
    /// <param name="onLanguageChanged">Optional callback when the user selects a different language.</param>
    public LanguageSelectorViewModel(
        IEnumerable<LanguageItem> languages,
        Action<CultureInfo>? onLanguageChanged = null)
    {
        AvailableLanguages = new ObservableCollection<LanguageItem>(languages ?? throw new ArgumentNullException(nameof(languages)));
        if (AvailableLanguages.Count == 0)
            throw new ArgumentException("At least one language must be provided.", nameof(languages));

        _selectedLanguage = GetInitialSelection();
        _onLanguageChanged = onLanguageChanged;
        _localization.LanguageChanged += OnLanguageChanged;
    }

    private static CultureInfo GetKurdishCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo("ku-Arab-IQ");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("ku-IQ");
        }
    }

    private LanguageItem GetInitialSelection()
    {
        var match = AvailableLanguages.FirstOrDefault(x =>
            string.Equals(x.Culture.Name, _localization.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase));
        if (match != null && match.IsEnabled)
            return match;
        return AvailableLanguages.FirstOrDefault(x => x.IsEnabled) ?? AvailableLanguages[0];
    }

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

    private LanguageItem _selectedLanguage;
    public LanguageItem SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value == null || _selectedLanguage == value)
                return;
            if (!value.IsEnabled)
                return;

            _selectedLanguage = value;
            _localization.SetCulture(value.Culture);
            _onLanguageChanged?.Invoke(value.Culture);
            RaisePropertyChanged(nameof(SelectedLanguage));
        }
    }

    private void OnLanguageChanged()
    {
        var newSelected = AvailableLanguages.FirstOrDefault(x =>
            string.Equals(x.Culture.Name, _localization.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase));

        if (newSelected == null || !newSelected.IsEnabled)
            newSelected = AvailableLanguages.FirstOrDefault(x => x.IsEnabled) ?? AvailableLanguages[0];

        if (newSelected != null && _selectedLanguage != newSelected)
        {
            _selectedLanguage = newSelected;
            RaisePropertyChanged(nameof(SelectedLanguage));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _localization.LanguageChanged -= OnLanguageChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
