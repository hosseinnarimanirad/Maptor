using IRI.Maptor.SampleMauiApp.ViewModels;

namespace IRI.Maptor.SampleMauiApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		BindingContext = new MainViewModel();
	}
}
