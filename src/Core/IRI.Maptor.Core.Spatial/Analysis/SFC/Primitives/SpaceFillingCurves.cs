using IRI.Maptor.Core.Common.Primitives; 

namespace IRI.Maptor.Core.Spatial.Analysis.SFC;

public static class SpaceFillingCurves
{
    public static List<Transform> GetHilbert1stToOtherSubregionTransforms(List<Move> bMFs)
    {
        Move third = Transforms.Rotate180(bMFs[0]);

        Point temp1 = bMFs[1](bMFs[0](new Point(0, 0), 1), 1);

        Point temp2 = third(new Point(0, 0), 1);

        bool isCCW = temp1.X * temp2.Y - temp2.X * temp1.Y > 0;

        List<Transform> transforms = new List<Transform>{
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(bMFs[0]),
                                                                            isCCW?Transforms.Rotate90CCW:Transforms.Rotate90CW),
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(bMFs[0]),
                                                                            isCCW?Transforms.Rotate90CCW:Transforms.Rotate90CW),
                                                    Transforms.Rotate180};

        return transforms;

    }

    public static List<Transform> GetSimple1stToOtherSubregionTransforms(List<Move> bMFs)
    {
        return new List<Transform>{Transforms.DoNothing,
                                    Transforms.DoNothing,Transforms.DoNothing,
                                    Transforms.DoNothing,Transforms.DoNothing,
                                    Transforms.DoNothing,Transforms.DoNothing};
    }

    public static List<Transform> GetGray1stToOtherSubregionTransforms(List<Move> bMFs)
    {
        return new List<Transform> { Transforms.Rotate180, Transforms.Rotate180, Transforms.DoNothing };
    }

    public static SpaceFillingCurve Hilbert(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        List<Move> movements = new List<Move> { first, second, third };

        Point temp1 = second(first(new Point(0, 0), 1), 1);

        Point temp2 = third(new Point(0, 0), 1);

        bool isCCW = temp1.X * temp2.Y - temp2.X * temp1.Y > 0;

        List<Transform> transforms = new List<Transform>{
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(first),
                                                                            isCCW?Transforms.Rotate90CCW:Transforms.Rotate90CW),
                                                    Transforms.DoNothing,
                                                    Transforms.DoNothing,
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(first),
                                                                            isCCW?Transforms.Rotate90CW:Transforms.Rotate90CCW)};

        return new SpaceFillingCurve(2, movements, transforms);
    }

    public static SpaceFillingCurve Hossein(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        List<Move> movements = new List<Move> { first, second, third };

        Point temp1 = second(first(new Point(0, 0), 1), 1);

        Point temp2 = third(new Point(0, 0), 1);

        bool isCCW = temp1.X * temp2.Y - temp2.X * temp1.Y > 0;

        List<Transform> transforms = new List<Transform>{
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(first),
                                                                            isCCW?Transforms.Rotate90CCW:Transforms.Rotate90CW),
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(first),
                                                                            isCCW?Transforms.Rotate90CCW:Transforms.Rotate90CW),
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(first),
                                                                            isCCW?Transforms.Rotate90CW:Transforms.Rotate90CCW),
                                                    Transforms.CompositeTransform(
                                                                            Transforms.Reflex(first),
                                                                            isCCW?Transforms.Rotate90CW:Transforms.Rotate90CCW)};

        return new SpaceFillingCurve(2, movements, transforms);
    }

    public static SpaceFillingCurve Gray(Move first, Move second, Move third)
    {
        List<Move> movements = new List<Move> { first, second, third };

        List<Transform> transforms = new List<Transform>{Transforms.DoNothing,
                                                            Transforms.Rotate180,
                                                            Transforms.Rotate180,
                                                            Transforms.DoNothing};

        return new SpaceFillingCurve(2, movements, transforms);
    }

    public static SpaceFillingCurve Moore(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        List<Move> movements = new List<Move> { first, second, third };

        Point temp1 = second(first(new Point(0, 0), 1), 1);

        Point temp2 = third(new Point(0, 0), 1);

        bool isCCW = temp1.X * temp2.Y - temp2.X * temp1.Y > 0;

        List<Transform> transforms = new List<Transform>
        {
            isCCW ? Transforms.Rotate90CW : Transforms.Rotate90CCW,
            isCCW ? Transforms.Rotate90CW : Transforms.Rotate90CCW,
            !isCCW ? Transforms.Rotate90CW : Transforms.Rotate90CCW,
            !isCCW ? Transforms.Rotate90CW : Transforms.Rotate90CCW
        };

        return new SpaceFillingCurve(2, movements, transforms);
    }

    public static SpaceFillingCurve Peano(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        List<Move> movements = new List<Move> { first, first, second, third, third, second, first, first };

        List<Transform> transforms =
            new List<Transform>{
                    Transforms.DoNothing,
                    Transforms.Reflex(first),
                    Transforms.DoNothing,
                    Transforms.Reflex(second),
                    Transforms.Rotate180,
                    Transforms.Reflex(second),
                    Transforms.DoNothing,
                    Transforms.Reflex(first),
                    Transforms.DoNothing};

        return new SpaceFillingCurve(3, movements, transforms);
    }

    public static SpaceFillingCurve NOrdering(Move first, Move second)
    {
        List<Move> movements = new List<Move> { first, second, first };

        List<Transform> transforms = new List<Transform> { Transforms.DoNothing, Transforms.DoNothing, Transforms.DoNothing, Transforms.DoNothing };

        return new SpaceFillingCurve(2, movements, transforms);

    }

    public static SpaceFillingCurve ZOrdering()
    {
        List<Move> movements = new List<Move> { Moves.North, Moves.SouthEast, Moves.North };

        List<Transform> transforms = new List<Transform> { Transforms.DoNothing, Transforms.DoNothing, Transforms.DoNothing, Transforms.DoNothing };

        return new SpaceFillingCurve(2, movements, transforms);
    }

    /// <summary>
    /// Wunderlich 
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static SpaceFillingCurve Peano02(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        List<Move> movements = new List<Move> { first, first, second, third, third, second, first, first };

        List<Transform> transforms =
                        new List<Transform>{
                            Transforms.DoNothing,
                            Transforms.Rotate90CCW,
                            Transforms.DoNothing,
                            Transforms.Rotate90CW,
                            Transforms.Rotate180,
                            Transforms.Rotate90CW,
                            Transforms.DoNothing,
                            Transforms.Rotate90CCW,
                            Transforms.DoNothing};

        return new SpaceFillingCurve(3, movements, transforms);

    }

    /// <summary>
    /// Peano-Meander
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static SpaceFillingCurve Peano03(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        Move fourth = Transforms.Rotate180(second);

        List<Move> movements = new List<Move> { first, first, second, second, third, fourth, third, second };

        List<Transform> transforms =
                        new List<Transform>{
                            Transforms.CompositeTransform(Transforms.Reflex(first),Transforms.Rotate90CW),
                            Transforms.CompositeTransform(Transforms.Reflex(first),Transforms.Rotate90CW),
                            Transforms.DoNothing,
                            Transforms.DoNothing,
                            Transforms.DoNothing,
                            Transforms.Rotate180,
                            Transforms.CompositeTransform(Transforms.Rotate90CW,Transforms.Reflex(first)),
                            Transforms.CompositeTransform(Transforms.Rotate90CW,Transforms.Reflex(first)),
                            Transforms.DoNothing};

        return new SpaceFillingCurve(3, movements, transforms);

    }

    public static SpaceFillingCurve LebesgueDiagonal(Move first, Move second)
    {
        Move third = Transforms.Reflex(second)(first);

        List<Move> movements = new List<Move> { first, second, third };

        List<Transform> transforms =
                        new List<Transform>{
                            Transforms.DoNothing,
                            Transforms.DoNothing,
                            Transforms.DoNothing,
                            Transforms.DoNothing};

        return new SpaceFillingCurve(2, movements, transforms);

    }

    public static SpaceFillingCurve UOrderOrLebesgueSquare(Move first, Move second)
    {
        Move third = Transforms.Rotate180(first);

        List<Move> movements = new List<Move> { first, second, third };

        List<Transform> transforms =
                        new List<Transform>{
                            Transforms.DoNothing,
                            Transforms.DoNothing,
                            Transforms.DoNothing,
                            Transforms.DoNothing};

        return new SpaceFillingCurve(2, movements, transforms);

    }
}
