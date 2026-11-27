using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>One part of a guide page: a main heading and the blocks up to the next one.</summary>
/// <param name="Title">The heading's text, shown in the guide's contents list.</param>
/// <param name="Heading">The heading paragraph; null for none (a page without headings is one section).</param>
public sealed record GuideSection(string Title, Paragraph? Heading);

/// <summary>
/// Splits a guide page into its sections at the paragraphs styled <see cref="HeadingStyleKey"/>, for the contents
/// list of <see cref="GuideWindow"/>. Subheadings stay inside their section.
/// </summary>
/// <remarks>
/// The blocks before the first heading (the title and the lead) open the first section, so the first page still
/// starts with what the tool is for.
/// </remarks>
public static class GuideSections
{
    /// <summary>The style key of a main heading in Assets/Styles/Guide.xaml.</summary>
    public const string HeadingStyleKey = "IRI.Maptor.Styles.Guide.Heading";

    /// <summary>The sections of <paramref name="document"/>, in reading order, without changing it.</summary>
    public static IReadOnlyList<GuideSection> Find(FlowDocument document)
    {
        var headingStyle = document.TryFindResource(HeadingStyleKey) as Style;

        var headings = headingStyle is null
            ? new List<Paragraph>()
            : document.Blocks.OfType<Paragraph>().Where(p => ReferenceEquals(p.Style, headingStyle)).ToList();

        if (headings.Count == 0)
            return new[] { new GuideSection(string.Empty, null) };

        return headings.Select(h => new GuideSection(TextOf(h), h)).ToList();
    }

    /// <summary>
    /// Moves the blocks of section <paramref name="index"/> (as found by <see cref="Find"/>) out of
    /// <paramref name="document"/> into a new page that shares its resources. The source document is consumed:
    /// split a fresh copy for each section.
    /// </summary>
    public static FlowDocument TakeSection(FlowDocument document, int index)
    {
        var sections = Find(document);

        var all = document.Blocks.ToList();

        int start = index == 0 || sections[index].Heading is null ? 0 : all.IndexOf(sections[index].Heading!);

        int end = index + 1 < sections.Count ? all.IndexOf(sections[index + 1].Heading!) : all.Count;

        var page = new FlowDocument();

        page.Resources.MergedDictionaries.Add(document.Resources);

        foreach (var block in all.GetRange(start, end - start))
        {
            document.Blocks.Remove(block);

            page.Blocks.Add(block);
        }

        return page;
    }

    private static string TextOf(Paragraph paragraph)
        => new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim();
}
