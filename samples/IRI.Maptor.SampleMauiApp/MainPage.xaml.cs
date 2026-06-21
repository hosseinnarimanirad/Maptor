using IRI.Maptor.Jab.Core.Localization;
using IRI.Maptor.Jab.Maui.Localization;
using IRI.Maptor.SampleMauiApp.ViewModels;

namespace IRI.Maptor.SampleMauiApp;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _viewModel = new();

	private static string L(string key) => LocalizationManager.Instance[key];

	public MainPage()
	{
		InitializeComponent();

		BindingContext = _viewModel;

		// Mirror the whole page for RTL languages now and on every language change.
		LocalizationFlow.Apply(this);

		Map.MapTapped += (_, _) => _viewModel.CloseSidebars();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		await _viewModel.LoadProjectsAsync();
	}

	private async void OnSearchClicked(object? sender, EventArgs e)
	{
		var text = await DisplayPromptAsync(
			L("dialog_search_title"),
			L("dialog_search_message"),
			placeholder: L("dialog_search_placeholder"),
			initialValue: _viewModel.LocationText);

		if (!string.IsNullOrWhiteSpace(text))
		{
			_viewModel.GoTo(text);
		}
	}

	private async void OnTakePhotoClicked(object? sender, EventArgs e)
	{
		try
		{
			if (!MediaPicker.Default.IsCaptureSupported)
			{
				_viewModel.StatusMessage = L("status_cameraUnavailable");
				return;
			}

			var photo = await MediaPicker.Default.CapturePhotoAsync();

			_viewModel.StatusMessage = photo is null
				? null // cancelled
				: string.Format(L("status_photoCaptured"), photo.FileName);
		}
		catch (Exception)
		{
			_viewModel.StatusMessage = L("status_photoError");
		}
	}

	private async void OnLanguageClicked(object? sender, EventArgs e)
	{
		var choice = await DisplayActionSheet(
			L("nav_languageMenuTitle"),
			L("nav_cancel"),
			null,
			L("lang_english"),
			L("lang_farsi"));

		if (choice == L("lang_english"))
		{
			App.SetLanguage("en-US");
		}
		else if (choice == L("lang_farsi"))
		{
			App.SetLanguage("fa-IR");
		}
	}
}
