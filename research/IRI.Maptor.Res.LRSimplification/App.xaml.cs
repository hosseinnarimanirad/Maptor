using IRI.Maptor.Presentation.Wpf.Localization;
using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Res.LRSimplification.Common;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace IRI.Maptor.Res.LRSimplification;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register app-specific resources for localization (chained ResourceManager)
        LocalizationManager.Instance.RegisterResourceManager(IRI.Maptor.Res.LRSimplification.Properties.Resources.ResourceManager);

        // headless mode: `--run <LrsExperiment>` runs one experiment without the window and exits, so a run
        // can be reproduced from the command line, e.g.
        //   dotnet run --project research\IRI.Maptor.Res.LRSimplification -- --run RevisionPublishedFeatures128
        var runIndex = Array.IndexOf(e.Args, "--run");

        // `--label-oracle [file]` renders and labels the synthetic grid with the rasterization oracle and exits
        var labelIndex = Array.IndexOf(e.Args, "--label-oracle");

        // `--export-labels [folder]` writes the synthetic label grids (label-grid-V23.json, -V24.json) and exits
        var exportIndex = Array.IndexOf(e.Args, "--export-labels");

        if (exportIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LRHelper.ExportLabelGrids(exportIndex + 1 < e.Args.Length && !e.Args[exportIndex + 1].StartsWith("--") ? e.Args[exportIndex + 1] : null);

            Shutdown();

            return;
        }

        if (labelIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                LRHelper.LabelOracle(labelIndex + 1 < e.Args.Length && !e.Args[labelIndex + 1].StartsWith("--") ? e.Args[labelIndex + 1] : null);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(LRHelper.OutputRoot, $"Error-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt"), ex.ToString());

                Environment.ExitCode = 1;
            }

            if (runIndex < 0)
            {
                Shutdown();

                return;
            }
        }

        if (runIndex >= 0)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var experiment = runIndex + 1 < e.Args.Length
                ? Enum.Parse<LrsExperiment>(e.Args[runIndex + 1], ignoreCase: true)
                : LrsExperiment.SingleFeatureVaryingThreshold128;

            try
            {
                await LRHelper.GeneralTest(experiment);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(LRHelper.OutputRoot, $"Error-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt"), ex.ToString());

                Environment.ExitCode = 1;
            }

            Shutdown();

            return;
        }

        // `--snapshot <geojson> <png> [level]` opens the window, runs the comparison panel on one feature, saves the window and exits
        var snapshotIndex = Array.IndexOf(e.Args, "--snapshot");

        if (snapshotIndex >= 0 && snapshotIndex + 2 < e.Args.Length)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var window = new MainWindow();
            window.Show();

            try
            {
                int? level = snapshotIndex + 3 < e.Args.Length && int.TryParse(e.Args[snapshotIndex + 3], out var l) ? l : null;

                await window.SnapshotAsync(e.Args[snapshotIndex + 1], e.Args[snapshotIndex + 2], level);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(LRHelper.OutputRoot, $"Error-{DateTime.Now:yyyy-MM-dd HH-mm-ss}.txt"), ex.ToString());

                Environment.ExitCode = 1;
            }

            Shutdown();

            return;
        }

        new MainWindow().Show();
    }
}
