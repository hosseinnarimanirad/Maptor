using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Propagation;

namespace IRI.Maptor.Tests.Satellite;

/// <summary>
/// <see cref="Sgp4Propagator"/> against the reference verification set of Vallado et al.,
/// "Revisiting Spacetrack Report #3" (AIAA 2006-6753); see Assets/Sgp4/README.md.
/// </summary>
public class Sgp4VerificationTest(Xunit.Abstractions.ITestOutputHelper output)
{
    private static readonly string AssetFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "Sgp4");

    /// <summary>The output is printed to 1e-8 km and 1e-9 km/s; a faithful port lands within rounding.</summary>
    private const double PositionToleranceKm = 1e-6;

    private const double VelocityToleranceKmS = 1e-8;

    private sealed record ExpectedRow(double Minutes, double X, double Y, double Z, double Vx, double Vy, double Vz);

    /// <summary>The expected blocks in file order: one per element set (a catalog number can occur twice).</summary>
    private static List<(int Catalog, List<ExpectedRow> Rows)> ReadExpected() => ReadExpected(out _);

    /// <summary>
    /// The expected blocks, and the rows the file appends after its last block without a header of their
    /// own: after the second 20413 block (which ends at 1,844,340 min) the time drops back to 120 min for a
    /// second run of 08195 (Molniya), from 120 to 2,880 min, all on one line.
    /// </summary>
    private static List<(int Catalog, List<ExpectedRow> Rows)> ReadExpected(out List<ExpectedRow> trailing08195)
    {
        var result = new List<(int, List<ExpectedRow>)>();

        List<ExpectedRow>? current = null;

        trailing08195 = new List<ExpectedRow>();

        foreach (var line in File.ReadLines(Path.Combine(AssetFolder, "tmatverDec2015.out")))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                continue;

            if (parts.Length == 2 && parts[1] == "xx")
            {
                current = new List<ExpectedRow>();
                result.Add((CatalogNumbers.Decode(parts[0]), current));
                continue;
            }

            double P(int i) => double.Parse(parts[i], CultureInfo.InvariantCulture);

            var row = new ExpectedRow(P(0), P(1), P(2), P(3), P(4), P(5), P(6));

            if (current!.Count > 0 && current[^1].Minutes > 1_000_000 && row.Minutes < current[^1].Minutes)
            {
                // the appended rows all sit on this one line, seven numbers each
                for (var k = 0; k + 6 < parts.Length; k += 7)
                    trailing08195.Add(new ExpectedRow(P(k), P(k + 1), P(k + 2), P(k + 3), P(k + 4), P(k + 5), P(k + 6)));

                continue;
            }

            current.Add(row);
        }

        return result;
    }

    private static List<OrbitalElements> ReadElements(out List<TleReadError> errors)
    {
        var list = new List<TleReadError>();

        var sets = TleReader.ReadFile(Path.Combine(AssetFolder, "SGP4-VER.TLE"), list.Add);

        errors = list;

        return sets;
    }

    /// <summary>
    /// The set's error-code cases (33333, 33334, 33335 — "check error code 4/2/3") are hand-edited
    /// copies of real TLEs whose checksums were not updated, so a careful reader must refuse them.
    /// </summary>
    private static readonly int[] HandEditedCases = { 33333, 33334, 33335 };

    [Fact]
    public void VerificationFile_IsReadCompletely_ExceptTheHandEditedErrorCases()
    {
        var sets = ReadElements(out var errors);

        Assert.Equal(3, errors.Count);
        Assert.All(errors, e => Assert.Contains("checksum", e.Message));
        Assert.Equal(HandEditedCases, errors.Select(e => CatalogNumbers.Decode(e.Line1!.Substring(2, 5))));

        Assert.Equal(30, sets.Count);
        Assert.Equal(33, ReadExpected().Count);
        Assert.Equal(21, sets.Count(e => Sgp4Propagator.IsDeepSpaceOrbit(e)));
    }

    [Fact]
    public void EveryNearEarthCase_MatchesTheReferenceOutput()
    {
        var expected = ReadExpected();

        var sets = ReadElements(out _);

        int nearEarthSets = 0, rows = 0;

        double worstPosition = 0, worstVelocity = 0;

        // the expected blocks follow the file's order; the hand-edited cases were not read, so skip theirs
        var blocks = expected.Where(b => !HandEditedCases.Contains(b.Catalog)).ToList();

        Assert.Equal(blocks.Count, sets.Count);

        for (int i = 0; i < sets.Count; i++)
        {
            var elements = sets[i];

            Assert.Equal(blocks[i].Catalog, elements.CatalogNumberValue);

            if (Sgp4Propagator.IsDeepSpaceOrbit(elements))
                continue;

            nearEarthSets++;

            var propagator = Sgp4Propagator.Create(elements);

            foreach (var row in blocks[i].Rows)
            {
                var result = propagator.PropagateMinutes(row.Minutes);

                var dr = Math.Max(Math.Abs(result.Position.X - row.X), Math.Max(Math.Abs(result.Position.Y - row.Y), Math.Abs(result.Position.Z - row.Z)));
                var dv = Math.Max(Math.Abs(result.Velocity.X - row.Vx), Math.Max(Math.Abs(result.Velocity.Y - row.Vy), Math.Abs(result.Velocity.Z - row.Vz)));

                worstPosition = Math.Max(worstPosition, dr);
                worstVelocity = Math.Max(worstVelocity, dv);

                Assert.True(dr < PositionToleranceKm,
                    FormattableString.Invariant($"{elements.CatalogNumber} at {row.Minutes} min: position off by {dr:E2} km"));
                Assert.True(dv < VelocityToleranceKmS,
                    FormattableString.Invariant($"{elements.CatalogNumber} at {row.Minutes} min: velocity off by {dv:E2} km/s"));

                rows++;
            }
        }

        output.WriteLine(FormattableString.Invariant(
            $"{nearEarthSets} near-Earth sets, {rows} rows; worst position {worstPosition:E2} km, worst velocity {worstVelocity:E2} km/s"));

        // the set's near-Earth cases: normal, low-perigee, simplified-drag, tiny-eccentricity and decaying orbits
        Assert.True(nearEarthSets >= 8, $"only {nearEarthSets} near-Earth sets were checked");
        Assert.True(rows >= 100, $"only {rows} rows were checked");
    }

    /// <summary>
    /// 23333 (WIND: eccentricity 0.97, a 13.7-day orbit) differs by 4 mm, the same in both modes: the last
    /// bits of the arithmetic, magnified by a nearly parabolic orbit. Every other row is within rounding.
    /// </summary>
    private const double Wind23333ToleranceKm = 1e-5;

    /// <summary>
    /// 23599 (inclination 6.9°) takes the Lyddane branch of dpper, where the two modes differ in the
    /// quadrant of the node. The reference output follows the AFSPC reading there (in Improved mode the
    /// difference reaches 1 km), while its sidereal time is the improved one: in AFSPC mode the
    /// resonant cases differ by up to 7e-8 km, in Improved by 5e-9 km. So 23599 is checked in AFSPC mode.
    /// </summary>
    private const int LyddaneQuadrantCase = 23599;

    /// <summary>The deep-space cases: 12- and 24-hour resonances, Molniya, geosynchronous, a backward-running case.</summary>
    [Theory]
    [InlineData(Sgp4OperationMode.Improved)]
    [InlineData(Sgp4OperationMode.Afspc)]
    public void EveryDeepSpaceCase_MatchesTheReferenceOutput(Sgp4OperationMode mode)
    {
        var blocks = ReadExpected(out var trailing08195).Where(b => !HandEditedCases.Contains(b.Catalog)).ToList();

        var sets = ReadElements(out _);

        int deepSets = 0, rows = 0;

        double worstPosition = 0, worstVelocity = 0;

        var failures = new List<string>();

        for (int i = 0; i < sets.Count; i++)
        {
            var elements = sets[i];

            if (!Sgp4Propagator.IsDeepSpaceOrbit(elements))
                continue;

            if (elements.CatalogNumberValue == LyddaneQuadrantCase && mode == Sgp4OperationMode.Improved)
                continue;

            deepSets++;

            var propagator = Sgp4Propagator.Create(elements, operationMode: mode);

            Assert.True(propagator.IsDeepSpace);

            double setWorst = 0;

            var tolerance = elements.CatalogNumberValue == 23333 ? Wind23333ToleranceKm : PositionToleranceKm;

            // the file's headerless second run of 08195
            var expectedRows = elements.CatalogNumberValue == 8195 ? blocks[i].Rows.Concat(trailing08195).ToList() : blocks[i].Rows;

            foreach (var row in expectedRows)
            {
                var result = propagator.PropagateMinutes(row.Minutes);

                var dr = Math.Max(Math.Abs(result.Position.X - row.X), Math.Max(Math.Abs(result.Position.Y - row.Y), Math.Abs(result.Position.Z - row.Z)));
                var dv = Math.Max(Math.Abs(result.Velocity.X - row.Vx), Math.Max(Math.Abs(result.Velocity.Y - row.Vy), Math.Abs(result.Velocity.Z - row.Vz)));

                worstPosition = Math.Max(worstPosition, dr);
                worstVelocity = Math.Max(worstVelocity, dv);
                setWorst = Math.Max(setWorst, dr);

                if (!(dr < tolerance) || !(dv < VelocityToleranceKmS))
                    failures.Add(FormattableString.Invariant($"{elements.CatalogNumber} at {row.Minutes} min: position off by {dr:E2} km, velocity by {dv:E2} km/s"));

                rows++;
            }

            output.WriteLine(FormattableString.Invariant($"{elements.CatalogNumber} ({propagator.PeriodMinutes:F0} min): {expectedRows.Count} rows, worst {setWorst:E2} km"));
        }

        output.WriteLine(FormattableString.Invariant(
            $"{mode}: {deepSets} deep-space sets, {rows} rows; worst position {worstPosition:E2} km, worst velocity {worstVelocity:E2} km/s"));

        Assert.True(failures.Count == 0, $"{failures.Count} rows off; first: " + string.Join(" | ", failures.Take(5)));
        Assert.True(deepSets >= 20, $"only {deepSets} deep-space sets were checked");
        Assert.Equal(24, trailing08195.Count);
        Assert.Equal(2880, trailing08195[^1].Minutes);
    }

    [Fact]
    public void DecayingSatellite_IsReportedOnceItIsBelowTheSurface()
    {
        // MINOTAUR R/B, sub-orbital (perigee −51 km): "lost in 50 minutes"
        var minotaur = ReadElements(out _).Single(e => e.CatalogNumberValue == 28872);

        var propagator = Sgp4Propagator.Create(minotaur);

        Assert.Equal(Sgp4Status.Ok, propagator.PropagateMinutes(0).Status);
        Assert.Equal(Sgp4Status.Decayed, propagator.PropagateMinutes(60).Status);
    }
}
