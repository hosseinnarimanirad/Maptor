using System;
using System.Threading;
using System.Windows;

namespace IRI.Maptor.Tst.Main.Common;

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
}

[CollectionDefinition(WpfCollection.Name)]
public class WpfCollection
{
    public const string Name = "WPF (single Application instance)";
}
