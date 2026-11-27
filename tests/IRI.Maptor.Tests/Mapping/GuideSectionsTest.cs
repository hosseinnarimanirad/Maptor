using System.Linq;
using System.Windows;
using System.Windows.Documents;

using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// GuideSections: a guide page is split at its main headings for the contents list of GuideWindow. Built from a
/// synthetic page whose heading style is a plain keyed style, as Guide.xaml provides it.
/// </summary>
[Collection(WpfCollection.Name)]
public class GuideSectionsTest
{
    private static FlowDocument Page(params string[] headings)
    {
        var document = new FlowDocument();

        var heading = new Style(typeof(Paragraph));
        var subheading = new Style(typeof(Paragraph));

        document.Resources[GuideSections.HeadingStyleKey] = heading;

        document.Blocks.Add(new Paragraph(new Run("Title")));
        document.Blocks.Add(new Paragraph(new Run("Lead")));

        foreach (var h in headings)
        {
            document.Blocks.Add(new Paragraph(new Run(h)) { Style = heading });
            document.Blocks.Add(new Paragraph(new Run(h + " body")));
            document.Blocks.Add(new Paragraph(new Run(h + " sub")) { Style = subheading });
            document.Blocks.Add(new Paragraph(new Run(h + " sub body")));
        }

        return document;
    }

    private static string[] Texts(FlowDocument document)
        => document.Blocks.OfType<Paragraph>().Select(p => new TextRange(p.ContentStart, p.ContentEnd).Text).ToArray();

    [Fact]
    public void Find_NamesOneSectionPerMainHeading_SubheadingsStayInside() => WpfTestHost.Run(() =>
    {
        var sections = GuideSections.Find(Page("Inputs", "How it works", "Example"));

        Assert.Equal(new[] { "Inputs", "How it works", "Example" }, sections.Select(s => s.Title));
        Assert.All(sections, s => Assert.NotNull(s.Heading));
    });

    [Fact]
    public void Find_PageWithoutHeadings_IsOneSection() => WpfTestHost.Run(() =>
    {
        var sections = GuideSections.Find(Page());

        Assert.Single(sections);
        Assert.Null(sections[0].Heading);
    });

    [Fact]
    public void TakeSection_FirstSectionKeepsTitleAndLead_OthersStartAtTheirHeading() => WpfTestHost.Run(() =>
    {
        var first = GuideSections.TakeSection(Page("A", "B", "C"), 0);
        Assert.Equal(new[] { "Title", "Lead", "A", "A body", "A sub", "A sub body" }, Texts(first));

        var middle = GuideSections.TakeSection(Page("A", "B", "C"), 1);
        Assert.Equal(new[] { "B", "B body", "B sub", "B sub body" }, Texts(middle));

        var last = GuideSections.TakeSection(Page("A", "B", "C"), 2);
        Assert.Equal(new[] { "C", "C body", "C sub", "C sub body" }, Texts(last));

        // the section page still finds the page's styles
        Assert.NotNull(last.TryFindResource(GuideSections.HeadingStyleKey));
    });
}
