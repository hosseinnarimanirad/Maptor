using System;
using System.IO;
using System.Windows;

namespace IRI.Maptor.Res.TrajectoryCompression
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // headless mode: `--run <Experiment>` runs one experiment of SimplificationHelper into a dated folder and
            // exits, so a run can be reproduced from the command line, e.g.
            //   dotnet run -c Release --project research\IRI.Maptor.Res.TrajectoryCompression -- --run SingleFeatures
            // without it the window opens and its button runs the published assessment
            var runIndex = Array.IndexOf(e.Args, "--run");

            if (runIndex < 0)
            {
                new MainWindow().Show();

                return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var experiment = runIndex + 1 < e.Args.Length
                ? Enum.Parse<SimplificationHelper.Experiment>(e.Args[runIndex + 1], ignoreCase: true)
                : SimplificationHelper.Experiment.SingleFeatures;

            try
            {
                await SimplificationHelper.GeneralTest(experiment);
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory(SimplificationHelper.OutputRoot);

                File.WriteAllText(Path.Combine(SimplificationHelper.OutputRoot, $"Error-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt"), ex.ToString());

                Environment.ExitCode = 1;
            }

            Shutdown();
        }
    }
}
