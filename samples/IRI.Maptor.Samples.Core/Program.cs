// Maptor core samples — a cookbook of small, self-contained programs.
//
//   dotnet run --project samples/IRI.Maptor.Samples.Core                 # list samples
//   dotnet run --project samples/IRI.Maptor.Samples.Core -- <id>         # run one sample
//   dotnet run --project samples/IRI.Maptor.Samples.Core -- all          # run every sample
//
// Each sample is one file with one [Sample]-attributed static method. Add a file, add the
// attribute, and it shows up here.

using System.Reflection;
using IRI.Maptor.Samples.Core.Runner;

var samples = Assembly.GetExecutingAssembly()
    .GetTypes()
    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
    .Select(m => (Method: m, Info: m.GetCustomAttribute<SampleAttribute>()))
    .Where(x => x.Info is not null)
    .OrderBy(x => x.Info!.Id, StringComparer.Ordinal)
    .ToList();

if (args.Length == 0)
{
    Console.WriteLine("Maptor core samples");
    Console.WriteLine();
    foreach (var (_, info) in samples)
        Console.WriteLine($"  {info!.Id,-34} {info.Title}");
    Console.WriteLine();
    Console.WriteLine("Run one:  dotnet run -- <id>      Run all:  dotnet run -- all");
    return 0;
}

var selected = args[0].Equals("all", StringComparison.OrdinalIgnoreCase)
    ? samples
    : samples.Where(x => x.Info!.Id.Equals(args[0], StringComparison.OrdinalIgnoreCase)).ToList();

if (selected.Count == 0)
{
    Console.Error.WriteLine($"Unknown sample '{args[0]}'. Run without arguments to list the samples.");
    return 1;
}

foreach (var (method, info) in selected)
{
    Console.WriteLine($"=== {info!.Id} — {info.Title}");
    Console.WriteLine();
    method.Invoke(null, null);
    Console.WriteLine();
}

return 0;
