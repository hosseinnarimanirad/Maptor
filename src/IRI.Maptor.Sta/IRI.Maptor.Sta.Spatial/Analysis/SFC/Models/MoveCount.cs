using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Analysis.SFC;

public struct MoveCount
{
    Move move;

    int count;

    public MoveCount(Move move, int count)
    {
        this.move = move;

        this.count = count;
    }

    public Point DoMove(Point point, int step)
    {
        return move(point, step);
    }

    public MoveCount Transform(Transform transform)
    {
        return new MoveCount(transform(move), count);
    }

    public Move GetMove()
    {
        return move;
    }

    public int Count
    {
        get { return count; }
        set { count = value; }
    }

    public void Increment()
    {
        count++;
    }
}