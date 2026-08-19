using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Sta.Versioning;
using Sta = IRI.Maptor.Sta;

namespace IRI.Maptor.Jab.Wpf.ViewModels.Versioning;

/// <summary>
/// Transport delegate bundle for the review views: Jab.Wpf stays HTTP-agnostic, the
/// hosting app supplies implementations (Saba wires these to VersioningWebApi with its
/// shared authenticated client). Every operation throws on failure; the view models turn
/// exceptions into <see cref="ShowMessage"/> calls.
/// </summary>
public class ReviewFunctions
{
    public Func<CancellationToken, Task<List<ReviewQueueItemDto>>> LoadQueueAsync { get; set; }

    public Func<long, CancellationToken, Task<CompetitionCompareDto>> LoadCompareAsync { get; set; }

    public Func<SelectWinnerRequestDto, Task> SelectWinnerAsync { get; set; }

    public Func<CloseNoWinnerRequestDto, Task> CloseNoWinnerAsync { get; set; }

    public Func<GroupProposalsRequestDto, Task<long>> GroupProposalsAsync { get; set; }

    public Func<long, Task> DismissSuggestionAsync { get; set; }

    public Func<BulkAcceptRequestDto, Task<List<BulkAcceptResultItemDto>>> BulkAcceptAsync { get; set; }

    /// <summary>User-facing message sink (host decides: dialog, snackbar, …).</summary>
    public Action<string> ShowMessage { get; set; } = _ => { };

    /// <summary>
    /// Map inspection hook: (live, proposal) in WebMercator — wire to the presenter's
    /// RequestShowGeometryComparison so the main map renders the pair.
    /// </summary>
    public Action<Sta.Spatial.Primitives.Geometry<Sta.Common.Primitives.Point>?, Sta.Spatial.Primitives.Geometry<Sta.Common.Primitives.Point>?>? ShowGeometryComparison { get; set; }
}
