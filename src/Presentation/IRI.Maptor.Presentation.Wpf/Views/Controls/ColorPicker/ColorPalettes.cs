using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace IRI.Maptor.Presentation.Wpf.Controls;

/// <summary>The fixed swatch sets of <see cref="MaptorColorPicker"/>'s Grid and Brand tabs.</summary>
public static class ColorPalettes
{
    /// <summary>Columns of <see cref="GridColors"/>.</summary>
    public const int GridColumns = 12;

    /// <summary>
    /// 12 × 10, row by row: a grey ramp from white to black, then nine rows of twelve hues from dark to
    /// light. All 120 colours differ.
    /// </summary>
    public static IReadOnlyList<Color> GridColors { get; } = Parse(
        "#FFFFFF #ECECEC #D7D6D6 #C2C2C2 #AEAEAE #999999 #858586 #6F6F72 #5C5B5C #464746 #333333 #000000",
        "#01374A #011D57 #20053C #230538 #3C071B #5B1200 #5F2200 #563000 #584000 #6A6B05 #4F5504 #263E0F",
        "#004D65 #012F7B #1A0A52 #450D59 #551029 #831404 #7B2900 #7A4A00 #785800 #8C8502 #757404 #37571A",
        "#016E8F #0142A9 #2C0977 #61187C #791A3D #B51A00 #AD3E00 #A96800 #A67B01 #C4BC00 #9BA40D #4E7A27",
        "#008CB4 #0056D6 #361A94 #7A219E #99244F #E32400 #DA5100 #D38301 #D19D01 #F5EC00 #C3D117 #669D34",
        "#00A1D8 #0061FD #4D22B2 #982ABC #B92D5D #FF4015 #FF6A00 #FFAB01 #FCC700 #FEFB41 #D9EC37 #76BB40",
        "#00C7FD #3A87FD #5E30EB #BE38F3 #E73B7A #FE6250 #FE8648 #FEB43F #FECB3E #FFF76B #E4EF65 #96D35F",
        "#52D6FD #74A7FF #864FFD #D357FE #EF719E #FF8C82 #FFA57E #FEC877 #FED977 #FFF994 #EAF28F #B1DD8B",
        "#93E3FD #A7C6FF #B18CFE #E292FE #F4A4C0 #FFBDAD #FFCCAB #FEDBA9 #FCE4A7 #FFFBB9 #F1F7B7 #CDE8B5",
        "#CBF0FF #D2E2FF #D8C9FF #EFCAFF #F9D3E0 #FEDCD9 #FFE4D6 #FFE7D6 #FEF1D5 #FCFBDD #F6FADB #DEEED4");

    /// <summary>Columns of <see cref="BrandColors"/>.</summary>
    public const int BrandColumns = 8;

    /// <summary>
    /// 6 × 8, row by row: moss, olive, ochre, clay and azure (four tints, the Maptor brand colour, three
    /// shades), then the paper → ink neutrals of the design system.
    /// </summary>
    public static IReadOnlyList<Color> BrandColors { get; } =
        Ramp("#4E8159").Concat(Ramp("#7A9455")).Concat(Ramp("#C08A2D")).Concat(Ramp("#B15B34")).Concat(Ramp("#2F6E8C"))
            .Concat(Parse("#FFFFFF #F4F1EA #ECE6D9 #DBD3C1 #C9BFA8 #948A73 #6B6350 #211C13"))
            .ToArray();

    private static Color[] Parse(params string[] rows) =>
        rows.SelectMany(r => r.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
            .Select(hex => ColorMath.TryParseHex(hex, out var c) ? c : throw new System.FormatException(hex))
            .ToArray();

    /// <summary>Tints toward white (86, 68, 48, 24 %), the colour itself, shades toward black (18, 36, 54 %).</summary>
    private static IEnumerable<Color> Ramp(string hex)
    {
        ColorMath.TryParseHex(hex, out var c);

        foreach (var t in new[] { 0.86, 0.68, 0.48, 0.24 })
            yield return Mix(c, Colors.White, t);

        yield return c;

        foreach (var t in new[] { 0.18, 0.36, 0.54 })
            yield return Mix(c, Colors.Black, t);
    }

    private static Color Mix(Color a, Color b, double t) => Color.FromRgb(
        (byte)System.Math.Round(a.R + (b.R - a.R) * t),
        (byte)System.Math.Round(a.G + (b.G - a.G) * t),
        (byte)System.Math.Round(a.B + (b.B - a.B) * t));
}
