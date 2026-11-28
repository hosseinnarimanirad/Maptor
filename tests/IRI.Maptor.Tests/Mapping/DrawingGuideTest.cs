using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Documents;

using IRI.Maptor.Presentation.Wpf.Controls;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// The guide of drawing, snapping and editing (Guides/Drawing*.xaml, opened from the sketch bar): its English and
/// Persian pages load, have the same sections and show the same figures in the same order, and every case of
/// <see cref="DrawingGuideFigureKind"/> has its figure.
/// </summary>
[Collection(WpfCollection.Name)]
public class DrawingGuideTest
{
    private const string Source = "/IRI.Maptor.Presentation.Wpf;component/Guides/Drawing";

    private static FlowDocument Load(string culture)
    {
        var path = GuideWindow.GetCandidatePaths(Source, new CultureInfo(culture)).First();
        var document = GuideWindow.LoadDocument(Source, new CultureInfo(culture));

        Assert.True(document is not null, $"no page at {path}");

        return document!;
    }

    private static IEnumerable<DrawingGuideFigureKind> Figures(IEnumerable<Block> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case BlockUIContainer { Child: DrawingGuideFigure figure }:
                    yield return figure.Kind;
                    break;

                case Section section:
                    foreach (var kind in Figures(section.Blocks))
                        yield return kind;
                    break;

                case Table table:
                    foreach (var kind in table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells).SelectMany(c => Figures(c.Blocks)))
                        yield return kind;
                    break;
            }
        }
    }

    private static string Title(FlowDocument document)
        => new TextRange(document.Blocks.First().ContentStart, document.Blocks.First().ContentEnd).Text;

    [Fact]
    public void PersianPage_IsItsOwnPage_NotTheEnglishOne() => WpfTestHost.Run(() =>
    {
        Assert.Contains("Drawing", Title(Load("en")));
        Assert.Contains("ترسیم", Title(Load("fa-IR")));
    });

    [Fact]
    public void BothPages_HaveTheSameSections() => WpfTestHost.Run(() =>
    {
        var english = GuideSections.Find(Load("en"));
        var persian = GuideSections.Find(Load("fa-IR"));

        Assert.Equal(6, english.Count);
        Assert.Equal(english.Count, persian.Count);
    });

    [Fact]
    public void BothPages_ShowEveryCaseOnce_InTheSameOrder() => WpfTestHost.Run(() =>
    {
        var english = Figures(Load("en").Blocks).ToList();
        var persian = Figures(Load("fa-IR").Blocks).ToList();

        Assert.Equal(english, persian);
        Assert.Equal(Enum.GetValues<DrawingGuideFigureKind>().OrderBy(k => k), english.OrderBy(k => k));
    });
}
