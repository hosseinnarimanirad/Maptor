using System.Text;
using System.Windows;

namespace IRI.Maptor.Samples.Wpf.HelloMap;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Shapefile .dbf files often use legacy code pages (e.g. Windows-1256 for Persian);
        // register the provider once so they can be read.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
