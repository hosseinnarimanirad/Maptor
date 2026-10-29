using System.IO;
using System.Windows;

namespace IRI.Maptor.Res.FastSimplification;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // headless mode: `--run ThesisVisual` or `--run ThesisTiming` runs one experiment of ThesisExperiments
        // into a dated folder and exits; without it the window of the published experiment opens
        var runIndex = Array.IndexOf(e.Args, "--run");

        if (runIndex < 0 || runIndex + 1 >= e.Args.Length)
        {
            new MainWindow().Show();

            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var experiment = e.Args[runIndex + 1];

        var writeFolder = Path.Combine(ThesisExperiments.OutputRoot, $"{DateTime.Now:yyyy-MM-dd}-{experiment}");

        try
        {
            switch (experiment)
            {
                case "ThesisVisual": await ThesisExperiments.VisualAsync(writeFolder); break;
                case "ThesisTiming": await Task.Run(() => ThesisExperiments.Timing(writeFolder)); break;
                default: throw new ArgumentException($"unknown experiment {experiment}");
            }
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(ThesisExperiments.OutputRoot);

            File.WriteAllText(Path.Combine(ThesisExperiments.OutputRoot, $"Error-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt"), ex.ToString());

            Environment.ExitCode = 1;
        }

        Shutdown();
    }
}

