using IRI.Maptor.Core.Common.Primitives; 

namespace IRI.Maptor.Core.Spatial.Analysis.SFC;

public delegate Move Transform(Move moveFunc);

public static class Transforms
{
    public static Transform DoNothing = new Transform(moveFunc => moveFunc);

    public static Transform Rotate90CW = new Transform((moveFunc) => new Move((point, step) => Rotate(point, step, moveFunc, -Math.PI / 2)));

    public static Transform Rotate90CCW = new Transform((moveFunc) => new Move((point, step) => Rotate(point, step, moveFunc, Math.PI / 2)));

    public static Transform Rotate180 = new Transform((moveFunc) => new Move((point, step) => Rotate(point, step, moveFunc, Math.PI)));

    public static Transform HorizontalReflection = new Transform((moveFunc) => new Move((point, step) => ReflectHorizontally(point, step, moveFunc)));

    public static Transform VerticalReflection = new Transform((moveFunc) => new Move((point, step) => ReflectVertically(point, step, moveFunc)));

    private static Point ReflectHorizontally(Point point, int step, Move moveFunc)
    {
        Point temp = moveFunc(point, step);

        double dy = temp.Y - point.Y;

        return new Point(temp.X, point.Y - dy);
    }

    private static Point ReflectVertically(Point point, int step, Move moveFunc)
    {
        Point temp = moveFunc(point, step);

        double dx = temp.X - point.X;

        return new Point(point.X - dx, temp.Y);
    }

    private static Point Rotate(Point point, int step, Move moveFunc, double rotationAngle)
    {
        Point temp = moveFunc(point, step);

        temp = new Point(temp.X - point.X, temp.Y - point.Y);

        temp = new Point(temp.X * (int)Math.Cos(rotationAngle) - temp.Y * (int)Math.Sin(rotationAngle), temp.X * (int)Math.Sin(rotationAngle) + temp.Y * (int)Math.Cos(rotationAngle));

        return new Point(point.X + temp.X, point.Y + temp.Y);
    }

    public static Transform Reflex(Move baseFunc)
    {
        return new Transform(moveFunc => new Move((point, step) => Reflex(point, step, moveFunc, baseFunc)));
    }

    public static Point Reflex(Point point, int step, Move moveFunc, Move baseFunc)
    {
        Point basePoint = new Point(0, 0);

        Point first = baseFunc(basePoint, step);

        Point second = moveFunc(basePoint, step);

        //delegate distanceToBase = (Point p) => Math.Sqrt(p.X * p.X + p.Y * p.Y);

        double firstNorm = Math.Sqrt(first.X * first.X + first.Y * first.Y);
        //
        //double secondNorm = Math.Sqrt(second.X * second.X + second.Y * second.Y);

        double coef = (first.X * second.X + first.Y * second.Y) / (firstNorm * firstNorm);

        double dx = coef * first.X - second.X;

        double dy = coef * first.Y - second.Y;

        return new Point(point.X + second.X + (int)Math.Round(2 * dx), point.Y + second.Y + (int)Math.Round(2 * dy));
    }

    public static Transform CompositeTransform(Transform firstTransform, Transform secondTransform)
    {
        return new Transform(moveFunc => secondTransform(firstTransform(moveFunc)));
    }

}
