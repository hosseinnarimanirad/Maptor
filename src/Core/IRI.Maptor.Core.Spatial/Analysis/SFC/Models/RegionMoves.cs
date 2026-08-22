using IRI.Maptor.Core.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Spatial.Analysis.SFC;

public class RegionMoves
{
    MoveCount ForwardIn1stD, ReverseIn1stD, ForwardIn2ndD, ReverseIn2ndD, JumpIn1stD, JumpIn2ndD, ContiguityIn1stD, ContiguityIn2ndD;

    int ForwardIn1stDCount
    {
        get { return ForwardIn1stD.Count; }
    }

    int ReverseIn1stDCount
    {
        get { return ReverseIn1stD.Count; }
    }

    int ForwardIn2ndDCount
    {
        get { return ForwardIn2ndD.Count; }
    }

    int ReverseIn2ndDCount
    {
        get { return ReverseIn2ndD.Count; }
    }

    int JumpIn1stDCount
    {
        get { return JumpIn1stD.Count; }
    }

    int JumpIn2ndDCount
    {
        get { return JumpIn2ndD.Count; }
    }

    int ContiguityIn1stDCount
    {
        get { return ContiguityIn1stD.Count; }
    }

    int ContiguityIn2ndDCount
    {
        get { return ContiguityIn2ndD.Count; }
    }

    int DirectionSegmentIn1stCount
    {
        get { return ForwardIn1stD.Count + ReverseIn1stD.Count; }
    }

    int DirectionSegmentIn2ndCount
    {
        get { return ForwardIn2ndD.Count + ReverseIn2ndD.Count; }
    }

    int DistanceSegmentIn1stCount
    {
        get { return JumpIn1stD.Count + ContiguityIn1stD.Count; }
    }

    int DistanceSegmentIn2ndCount
    {
        get { return JumpIn2ndD.Count + ContiguityIn2ndD.Count; }
    }

    public RegionMoves()
    {
        ForwardIn1stD = new MoveCount((p, s) => new Point(p.X + 1, p.Y), 0);

        ReverseIn1stD = new MoveCount((p, s) => new Point(p.X - 1, p.Y), 0);

        ForwardIn2ndD = new MoveCount((p, s) => new Point(p.X, p.Y + 1), 0);

        ReverseIn2ndD = new MoveCount((p, s) => new Point(p.X, p.Y - 1), 0);
    }

    public void Add(SpaceFillingCurve sFC)
    {
        for (int i = 0; i < sFC.NumberOfSteps - 1; i++)
        {
            Add(sFC[i]);
        }
    }

    public void Add(Move move)
    {
        Add(move, 1);
    }

    public void Add(Move move, int count)
    {
        Point temp = move(new Point(0, 0), 1);

        if (temp.X > 0)
        {
            ForwardIn1stD.Count += count;
        }

        else if (temp.X < 0)
        {
            ReverseIn1stD.Count += count;
        }

        //
        if (temp.Y > 0)
        {
            ForwardIn2ndD.Count += count;
        }

        else if (temp.Y < 0)
        {
            ReverseIn2ndD.Count += count;
        }

        //
        if (Math.Abs(temp.X) > 1)
        {
            JumpIn1stD.Count += count;
        }
        else if (Math.Abs(temp.X) == 1)
        {
            ContiguityIn1stD.Count += count;
        }

        if (Math.Abs(temp.Y) > 1)
        {
            JumpIn2ndD.Count += count;
        }
        else if (Math.Abs(temp.Y) == 1)
        {
            ContiguityIn2ndD.Count += count;
        }
    }

    public void SetDirectionSegmentCount(int forwardIn1stDCount, int reverseIn1stDCount, int forwardIn2ndDCount, int reverseIn2ndDCount)
    {
        ForwardIn1stD.Count = forwardIn1stDCount;
        ReverseIn1stD.Count = reverseIn1stDCount;
        ForwardIn2ndD.Count = forwardIn2ndDCount;
        ReverseIn2ndD.Count = reverseIn2ndDCount;
    }

    public void SetDistanceSegmentCount(int jumpIn1stDCount, int contiguityIn1stDCount, int jumpIn2ndDCount, int contiguityIn2ndDCount)
    {
        JumpIn1stD.Count = jumpIn1stDCount;
        ContiguityIn1stD.Count = contiguityIn1stDCount;
        JumpIn2ndD.Count = jumpIn2ndDCount;
        ContiguityIn2ndD.Count = contiguityIn2ndDCount;
    }

    public RegionMoves Transform(Transform transform)
    {
        RegionMoves newRegionMoves = new RegionMoves();

        if (ForwardIn1stD.Count > 0)
        {
            newRegionMoves.Add(transform(ForwardIn1stD.GetMove()), ForwardIn1stD.Count);
        }

        if (ReverseIn1stD.Count > 0)
        {
            newRegionMoves.Add(transform(ReverseIn1stD.GetMove()), ReverseIn1stD.Count);
        }

        if (ForwardIn2ndD.Count > 0)
        {
            newRegionMoves.Add(transform(ForwardIn2ndD.GetMove()), ForwardIn2ndD.Count);
        }

        if (ReverseIn2ndD.Count > 0)
        {
            newRegionMoves.Add(transform(ReverseIn2ndD.GetMove()), ReverseIn2ndD.Count);
        }

        return newRegionMoves;
    }

    public static RegionMoves Combine(RegionMoves first, RegionMoves second)
    {
        RegionMoves newRegionMoves = new RegionMoves();

        newRegionMoves.SetDirectionSegmentCount(first.ForwardIn1stD.Count + second.ForwardIn1stD.Count,
                                            first.ReverseIn1stD.Count + second.ReverseIn1stD.Count,
                                            first.ForwardIn2ndD.Count + second.ForwardIn2ndD.Count,
                                            first.ReverseIn2ndD.Count + second.ReverseIn2ndD.Count);

        newRegionMoves.SetDistanceSegmentCount(first.JumpIn1stD.Count + second.JumpIn1stD.Count,
                                                first.ContiguityIn1stD.Count + second.ContiguityIn1stD.Count,
                                                first.JumpIn2ndD.Count + second.JumpIn2ndD.Count,
                                                first.ContiguityIn2ndD.Count + second.ContiguityIn2ndD.Count);

        return newRegionMoves;
    }
}