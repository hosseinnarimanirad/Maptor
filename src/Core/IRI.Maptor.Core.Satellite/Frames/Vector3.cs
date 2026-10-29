namespace IRI.Maptor.Core.Satellite.Frames;

/// <summary>A Cartesian vector: a position in km or a velocity in km/s.</summary>
public readonly struct Vector3
{
    public double X { get; }

    public double Y { get; }

    public double Z { get; }

    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator *(double k, Vector3 v) => new(k * v.X, k * v.Y, k * v.Z);

    public override string ToString() => FormattableString.Invariant($"({X:F6}, {Y:F6}, {Z:F6})");
}
