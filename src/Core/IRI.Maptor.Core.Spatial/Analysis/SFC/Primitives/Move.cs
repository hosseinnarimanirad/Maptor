using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Spatial.Analysis.SFC;

public delegate Point Move(Point point, int step);

public static class Moves
{
    public static Move West = new Move((point, step) => new Point(point.X - step, point.Y));

    public static Move East = new Move((point, step) => new Point(point.X + step, point.Y));

    public static Move North = new Move((point, step) => new Point(point.X, point.Y + step));

    public static Move South = new Move((point, step) => new Point(point.X, point.Y - step));

    public static Move NorthWest = new Move((point, step) => West(North(point, step), step));

    public static Move NorthEast = new Move((point, step) => East(North(point, step), step));

    public static Move SouthWest = new Move((point, step) => West(South(point, step), step));

    public static Move SouthEast = new Move((point, step) => East(South(point, step), step));
}
