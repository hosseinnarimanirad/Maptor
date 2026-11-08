using System;
using System.Threading;
using System.Windows;

namespace IRI.Maptor.Tests.Common;

/// <summary>
/// Runs WPF-dependent test code on an STA thread with a live <see cref="Application"/>, which
/// pack:// resource URIs (e.g. the packaged IRANSans font) need in order to resolve.
/// <para>
/// Calls are serialized on a global lock: <see cref="Application"/> is a per-AppDomain singleton,
/// so two test classes racing to construct one would throw. Tests using this should also share
/// the <see cref="WpfCollection"/> so xunit doesn't run them in parallel.
/// </para>
/// </summary>
internal static class WpfTestHost
{
    private static readonly object _gate = new();

    public static void Run(Action action)
    {
        Exception? failure = null;

        lock (_gate)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    if (Application.Current is null)
                        _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        if (failure is not null)
            throw new InvalidOperationException($"WPF test body threw: {failure}", failure);
    }

    /// <summary>
    /// Merges the MahApps control styles and a theme into the test <see cref="Application"/>, so a view whose own
    /// dictionaries build on them (Controls.All.xaml) can be constructed. Call inside <see cref="Run"/>.
    /// </summary>
    public static void EnsureMahAppsResources()
    {
        var resources = Application.Current.Resources;

        if (resources.Contains("MahApps.Styles.Button"))
            return;

        foreach (var source in new[]
                 {
                     "pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml",
                     "pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml",
                     "pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml",
                 })
        {
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Absolute) });
        }
    }
}

[CollectionDefinition(WpfCollection.Name)]
public class WpfCollection
{
    public const string Name = "WPF (single Application instance)";
}
