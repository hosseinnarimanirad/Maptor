using System.Globalization;

using IRI.Maptor.Jab.Core.Localization;

namespace IRI.Maptor.SampleMauiApp;

public partial class App : Application
{
	/// <summary>Preferences key under which the chosen UI language is remembered.</summary>
	public const string LanguagePreferenceKey = "app_language";

	// First launch defaults to Farsi (RTL); the user can switch and the choice is remembered.
	public const string DefaultLanguage = "fa-IR";

	public App()
	{
		InitializeComponent();

		// Set the saved language before the first page is built so the initial render is
		// already in the right language/direction. FlowDirection is applied on the page itself.
		ApplySavedLanguage();

		MainPage = new AppShell();
	}

	private static void ApplySavedLanguage()
	{
		var code = Preferences.Default.Get(LanguagePreferenceKey, DefaultLanguage);

		ApplyCulture(code);
	}

	/// <summary>
	/// Switches the UI language at runtime and remembers the choice. Bound strings refresh via the
	/// LocalizationManager's PropertyChanged(null), and FlowDirection updates via LocalizationFlow —
	/// so no page rebuild or restart is needed.
	/// </summary>
	public static void SetLanguage(string code)
	{
		Preferences.Default.Set(LanguagePreferenceKey, code);

		ApplyCulture(code);
	}

	private static void ApplyCulture(string code)
	{
		try
		{
			LocalizationManager.Instance.SetCulture(CultureInfo.GetCultureInfo(code));
		}
		catch (CultureNotFoundException)
		{
			LocalizationManager.Instance.SetCulture(CultureInfo.GetCultureInfo(DefaultLanguage));
		}
	}
}
