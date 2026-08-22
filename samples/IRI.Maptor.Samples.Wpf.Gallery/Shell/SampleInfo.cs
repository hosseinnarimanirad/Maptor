using System;
using System.Windows.Controls;

namespace IRI.Maptor.Samples.Wpf.Gallery.Shell;

/// <summary>One entry in the gallery: where it is, what it shows, and how to create its view.</summary>
public sealed class SampleInfo(string category, string title, string description, string folder, Func<UserControl> createView)
{
    private UserControl? _view;

    /// <summary>Group header in the navigation list.</summary>
    public string Category { get; } = category;

    public string Title { get; } = title;

    /// <summary>One or two sentences shown above the sample.</summary>
    public string Description { get; } = description;

    /// <summary>Folder name under <c>Samples/</c>; the README and the source live there.</summary>
    public string Folder { get; } = folder;

    public string SourceUrl =>
        $"https://github.com/hosseinnarimanirad/Maptor/tree/master/samples/IRI.Maptor.Samples.Wpf.Gallery/Samples/{Folder}";

    /// <summary>The sample's view, created on first use and kept alive so switching back is instant.</summary>
    public UserControl View => _view ??= createView();

    public override string ToString() => Title;
}
