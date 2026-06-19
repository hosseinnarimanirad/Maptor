using IRI.Maptor.SampleMauiApp.ViewModels;

namespace IRI.Maptor.SampleMauiApp;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _viewModel = new();

	public MainPage()
	{
		InitializeComponent();

		BindingContext = _viewModel;
	}

	private async void OnSearchClicked(object? sender, EventArgs e)
	{
		var text = await DisplayPromptAsync(
			"Search location",
			"Enter coordinates as 'lat, lon'",
			placeholder: "35.6892, 51.3890",
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
				_viewModel.StatusMessage = "Camera is not available on this device.";
				return;
			}

			var photo = await MediaPicker.Default.CapturePhotoAsync();

			_viewModel.StatusMessage = photo is null
				? null // cancelled
				: $"Photo captured: {photo.FileName}";
		}
		catch (Exception)
		{
			_viewModel.StatusMessage = "Could not capture a photo. Check camera permissions.";
		}
	}

	private async void OnMoreClicked(object? sender, EventArgs e)
	{
		var choice = await DisplayActionSheet(
			"More options",
			"Cancel",
			null,
			"Add GeoJSON…",
			"Load sample");

		switch (choice)
		{
			case "Add GeoJSON…":
				_viewModel.AddGeoJsonCommand.Execute(null);
				break;
			case "Load sample":
				_viewModel.LoadSampleCommand.Execute(null);
				break;
		}
	}
}
