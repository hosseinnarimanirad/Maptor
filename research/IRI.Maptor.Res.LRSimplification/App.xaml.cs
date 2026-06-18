using IRI.Maptor.Jab.Common.Localization;
using IRI.Maptor.Jab.Core.Localization;
using System.Configuration;
using System.Data;
using System.Windows;

namespace IRI.Maptor.Res.LRSimplification;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register app-specific resources for localization (chained ResourceManager)
        LocalizationManager.Instance.RegisterResourceManager(IRI.Maptor.Res.LRSimplification.Properties.Resources.ResourceManager);

    }
}

