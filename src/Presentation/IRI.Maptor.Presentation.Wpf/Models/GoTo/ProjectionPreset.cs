using System;
using System.Collections.Generic;

using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Core.SpatialReferenceSystem;
using IRI.Maptor.Core.SpatialReferenceSystem.MapProjections;

using Ellipsoid = IRI.Maptor.Core.SpatialReferenceSystem.Ellipsoid<IRI.Maptor.Core.Common.Metrics.Meter, IRI.Maptor.Core.Common.Metrics.Degree>;

namespace IRI.Maptor.Presentation.Wpf.Models.GoTo;

public enum ProjectionKind
{
    WebMercator,
    Mercator,
    TransverseMercator,
    LambertConformalConic2P,
    LambertConformalConic1P,
    CylindricalEqualArea,
}

/// <summary>
/// The defining constants of a conic or cylindrical projection, in degrees and metres.
/// </summary>
public sealed record ProjectionParameters(
    double CentralMeridian,
    double LatitudeOfOrigin,
    double ScaleFactor,
    double FalseEasting,
    double FalseNorthing,
    double StandardParallel1,
    double StandardParallel2)
{
    public static ProjectionParameters Empty { get; } = new(0, 0, 1, 0, 0, 0, 0);

    public static ProjectionParameters From(MapProjectionBase projection) => new(
        projection.CentralMeridian,
        projection.LatitudeOfOrigin,
        projection.ScaleFactor,
        projection.FalseEasting,
        projection.FalseNorthing,
        projection.StandardParallel1,
        projection.StandardParallel2);
}

/// <summary>
/// One entry of the "Projected" picker: either a named, fully specified system (Web
/// Mercator, the NIOC Lambert grid, …) or a projection family whose ellipsoid and
/// parameters the user fills in.
/// </summary>
public sealed class ProjectionPreset
{
    private readonly Func<Ellipsoid, ProjectionParameters, SrsBase> _factory;

    private ProjectionPreset(
        string key,
        string title,
        string? subtitle,
        ProjectionKind kind,
        Ellipsoid defaultEllipsoid,
        ProjectionParameters defaults,
        bool allowsEllipsoid,
        bool allowsParameters,
        Func<Ellipsoid, ProjectionParameters, SrsBase> factory)
    {
        Key = key;
        Title = title;
        Subtitle = subtitle;
        Kind = kind;
        DefaultEllipsoid = defaultEllipsoid;
        Defaults = defaults;
        AllowsEllipsoid = allowsEllipsoid;
        AllowsParameters = allowsParameters;
        _factory = factory;
    }

    public string Key { get; }

    /// <summary>Localized display name.</summary>
    public string Title { get; }

    /// <summary>Latin qualifier shown after the title: EPSG code, datum, or "custom".</summary>
    public string? Subtitle { get; }

    public ProjectionKind Kind { get; }

    public Ellipsoid DefaultEllipsoid { get; }

    public ProjectionParameters Defaults { get; }

    /// <summary>The user may pick the ellipsoid; false for systems whose datum is part of the definition.</summary>
    public bool AllowsEllipsoid { get; }

    /// <summary>The user may edit the projection constants; false for named systems.</summary>
    public bool AllowsParameters { get; }

    /// <summary>Transverse Mercator and Lambert expose constants; Mercator-family projections have none.</summary>
    public bool HasParameters => Kind is ProjectionKind.TransverseMercator or ProjectionKind.LambertConformalConic2P or ProjectionKind.LambertConformalConic1P;

    public bool HasStandardParallels => Kind == ProjectionKind.LambertConformalConic2P;

    public bool HasScaleFactor => Kind is ProjectionKind.TransverseMercator or ProjectionKind.LambertConformalConic2P or ProjectionKind.LambertConformalConic1P;

    public SrsBase CreateSrs(Ellipsoid ellipsoid, ProjectionParameters parameters) => _factory(ellipsoid, parameters);

    public override string ToString() => Subtitle is null ? Title : $"{Title} · {Subtitle}";

    /// <summary>
    /// The catalogue shown in the picker. Built on demand so the titles follow the UI language
    /// current when the dialog opens.
    /// </summary>
    public static List<ProjectionPreset> CreateDefaults()
    {
        var l = LocalizationManager.Instance;

        string tm = l["srs_tmTitle"];
        string lcc = l["srs_lccTitle"];
        string custom = l["dialog_goto_projectionCustom"];

        var nioc = SrsBases.LccNiocWithClarke1880Rgs;
        var fd58 = SrsBases.LccFd58;
        var nahrwan = SrsBases.LccNahrawanIraq;

        return new List<ProjectionPreset>
        {
            new ProjectionPreset(
                "webMercator", l["srs_webMercatorTitle"], "EPSG:3857",
                ProjectionKind.WebMercator, Ellipsoids.WGS84, ProjectionParameters.Empty,
                allowsEllipsoid: false, allowsParameters: false,
                (_, _) => SrsBases.WebMercator),

            new ProjectionPreset(
                "mercator", l["srs_mercatorTitle"], null,
                ProjectionKind.Mercator, Ellipsoids.WGS84, ProjectionParameters.Empty,
                allowsEllipsoid: true, allowsParameters: false,
                (e, _) => new Mercator(e, e.AreTheSame(Ellipsoids.WGS84) ? SridHelper.Mercator : 0)),

            new ProjectionPreset(
                "tm", tm, custom,
                ProjectionKind.TransverseMercator, Ellipsoids.WGS84,
                // zone 39 constants: the most common national sheet grid for Maptor's users
                new ProjectionParameters(51, 0, 0.9996, 500000, 0, 0, 0),
                allowsEllipsoid: true, allowsParameters: true,
                (e, p) => new TransverseMercator(e, p.CentralMeridian, p.LatitudeOfOrigin, p.FalseEasting, p.FalseNorthing, p.ScaleFactor)),

            new ProjectionPreset(
                "lcc", lcc, custom,
                ProjectionKind.LambertConformalConic2P, Ellipsoids.WGS84,
                ProjectionParameters.From(SrsBases.LccNiocWithWgs84),
                allowsEllipsoid: true, allowsParameters: true,
                (e, p) => new LambertConformalConic2P(e, p.StandardParallel1, p.StandardParallel2, p.CentralMeridian, p.LatitudeOfOrigin, p.FalseEasting, p.FalseNorthing, p.ScaleFactor)),

            new ProjectionPreset(
                "lccNioc", lcc, "NIOC · Clarke 1880 RGS",
                ProjectionKind.LambertConformalConic2P, nioc.Ellipsoid, ProjectionParameters.From(nioc),
                allowsEllipsoid: false, allowsParameters: false,
                (_, _) => nioc),

            new ProjectionPreset(
                "lccFd58", lcc, "FD58 · EPSG:3200",
                ProjectionKind.LambertConformalConic1P, fd58.Ellipsoid, ProjectionParameters.From(fd58),
                allowsEllipsoid: false, allowsParameters: false,
                (_, _) => fd58),

            new ProjectionPreset(
                "lccNahrwan", lcc, "Nahrwan 1967 Iraq",
                ProjectionKind.LambertConformalConic2P, nahrwan.Ellipsoid, ProjectionParameters.From(nahrwan),
                allowsEllipsoid: false, allowsParameters: false,
                (_, _) => nahrwan),

            new ProjectionPreset(
                "cea", l["srs_ceaTitle"], null,
                ProjectionKind.CylindricalEqualArea, Ellipsoids.WGS84, ProjectionParameters.Empty,
                allowsEllipsoid: true, allowsParameters: false,
                (e, _) => new CylindricalEqualArea(string.Empty, e, e.AreTheSame(Ellipsoids.WGS84) ? SridHelper.CylindricalEqualArea : 0)),
        };
    }
}
