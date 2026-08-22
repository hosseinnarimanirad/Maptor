namespace IRI.Maptor.Presentation.Wpf.Models.GoTo;

/// <summary>
/// The three families of input the Go To dialog resolves. The user picks one entry of
/// <see cref="CoordinateSystemOption"/>; this is the family that entry belongs to.
/// </summary>
public enum GoToMode
{
    Geodetic = 0,
    Utm = 1,
    Projected = 2,
}

public enum GeodeticFormat
{
    DegreesMinutesSeconds = 0,
    DecimalDegrees = 1,
}
