using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using IRI.Maptor.Core.Common.Enums;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Presentation.Wpf.Services;
using IRI.Maptor.Presentation.Wpf.ViewModels;
using IRI.Maptor.Tests.Common;

namespace IRI.Maptor.Tests.Mapping;

/// <summary>
/// MapViewModelBase.GetGeometryToEditAsync, the check before an edit starts (attribute-table Edit, drawing item Edit):
/// valid or orientation-only geometries go to the editor as they are, repairable ones only after "Fix and edit?",
/// unrepairable ones never (with a message). Synthetic Web Mercator geometries.
/// </summary>
[Collection(WpfCollection.Name)]
public class EditGeometryCheckTest
{
    private const int WebMercator = 3857;

    private sealed class TestMap : MapViewModelBase
    {
    }

    /// <summary>Records the yes/no questions and messages; answers every question with <see cref="Answer"/>.</summary>
    public class DialogRecorder : DispatchProxy
    {
        public bool? Answer { get; set; }

        public List<string> Questions { get; } = new();

        public List<string> Messages { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case nameof(IDialogService.ShowYesNoDialogAsync):
                    Questions.Add((string)args![0]!);
                    return Task.FromResult(Answer);

                case nameof(IDialogService.ShowMessageAsync):
                    Messages.Add((string)args![0]!);
                    return Task.CompletedTask;

                default:
                    throw new NotSupportedException(targetMethod?.Name);
            }
        }
    }

    private static Geometry<Point> Polygon(params (double x, double y)[] ring)
        => Geometry<Point>.Create(
            new List<Geometry<Point>> { Geometry<Point>.Create(ring.Select(p => new Point(p.x, p.y)).ToList(), GeometryType.LineString, WebMercator) },
            GeometryType.Polygon,
            WebMercator);

    private static (Geometry<Point>? Result, DialogRecorder Dialogs) Check(Geometry<Point> geometry, bool? answer)
    {
        Geometry<Point>? result = null;

        var dialogService = DispatchProxy.Create<IDialogService, DialogRecorder>();

        var recorder = (DialogRecorder)(object)dialogService;

        recorder.Answer = answer;

        WpfTestHost.Run(() =>
        {
            var map = new TestMap { DialogService = dialogService };

            result = map.GetGeometryToEditAsync(geometry).GetAwaiter().GetResult();
        });

        return (result, recorder);
    }

    [Fact]
    public void ValidGeometry_IsEditedAsItIs()
    {
        var geometry = Polygon((0, 0), (10, 0), (10, 10), (0, 10));

        var (result, dialogs) = Check(geometry, answer: true);

        Assert.Same(geometry, result);
        Assert.Empty(dialogs.Questions);
        Assert.Empty(dialogs.Messages);
    }

    [Fact]
    public void WrongRingDirectionOnly_IsEditedAsItIs()
    {
        var geometry = Polygon((0, 0), (0, 10), (10, 10), (10, 0));

        var (result, dialogs) = Check(geometry, answer: true);

        Assert.Same(geometry, result);
        Assert.Empty(dialogs.Questions);
    }

    [Fact]
    public void ClosedRing_Yes_EditsTheRepairedGeometry()
    {
        var geometry = Polygon((0, 0), (10, 0), (10, 10), (0, 10), (0, 0));

        var (result, dialogs) = Check(geometry, answer: true);

        Assert.Single(dialogs.Questions);
        Assert.NotNull(result);
        Assert.NotSame(geometry, result);
        Assert.Equal(4, result!.Geometries[0].Points.Count);
        Assert.Equal(100, result.EuclideanArea, 6);

        // the stored geometry is not touched until the edit is finished and applied
        Assert.Equal(5, geometry.Geometries[0].Points.Count);
    }

    [Fact]
    public void ClosedRing_No_DoesNotEdit()
    {
        var (result, dialogs) = Check(Polygon((0, 0), (10, 0), (10, 10), (0, 10), (0, 0)), answer: false);

        Assert.Single(dialogs.Questions);
        Assert.Null(result);
    }

    [Fact]
    public void Unrepairable_ShowsWhy_AndDoesNotEdit()
    {
        var (result, dialogs) = Check(Polygon((0, 0), (10, double.NaN), (10, 10), (0, 10)), answer: true);

        Assert.Null(result);
        Assert.Empty(dialogs.Questions);
        Assert.Single(dialogs.Messages);
    }
}
