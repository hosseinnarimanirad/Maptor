// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Sta.Spatial.Topology;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.Primitives;

public class QuasiTriangle
{
    private int first, second, third;

    private readonly int code;

    private int firstSecondNeighbour, secondThirdNeighbour, thirdFirstNeighbour;

    //directed arrow from first to second edge
    public static readonly int firstEdgeWeight = 1;

    //directed arrow from second to third edge
    public static readonly int secondEdgeWeight = 3;

    //directed arrow from third to first edge
    public static readonly int thirdEdgeWeight = 9;

    public int First
    {
        get { return first; }
        set { first = value; }
    }

    public int Second
    {
        get { return second; }
        set { second = value; }
    }

    public int Third
    {
        get { return third; }
        set { third = value; }
    }

    public int FirstSecondNeighbour
    {
        get { return firstSecondNeighbour; }
        set { firstSecondNeighbour = value; }
    }

    public int SecondThirdNeighbour
    {
        get { return secondThirdNeighbour; }
        set { secondThirdNeighbour = value; }
    }

    public int ThirdFirstNeighbour
    {
        get { return thirdFirstNeighbour; }
        set { thirdFirstNeighbour = value; }
    }

    public int[] OrderedNeighbours
    {
        get { return new int[] { FirstSecondNeighbour, SecondThirdNeighbour, ThirdFirstNeighbour }; }
    }

    public QuasiTriangle()
    {
        firstSecondNeighbour = -1;

        secondThirdNeighbour = -1;

        thirdFirstNeighbour = -1;
    }

    public QuasiTriangle(int first, int second, int third, int code)
    {
        this.code = code;

        this.first = first;

        this.second = second;

        this.third = third;

        firstSecondNeighbour = -1;

        secondThirdNeighbour = -1;

        thirdFirstNeighbour = -1;
    }

    public override string ToString()
    {
        return string.Format("{0} {1} {2}", first, second, third);
    }

    public override int GetHashCode()
    {
        return code;
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(QuasiTriangle))
        {
            QuasiTriangle tempTriangle = (QuasiTriangle)obj;

            return tempTriangle.First == First &&
                    tempTriangle.Second == Second &&
                    tempTriangle.Third == Third;
        }

        return false;
    }

    public bool HasThePoint(int pointCode)
    {
        return first == pointCode ||
                second == pointCode ||
                third == pointCode;
    }

    public TriangleRelation GetRelationTo(QuasiTriangle triangle)
    {
        int hasTheFirst = Convert.ToInt32(triangle.HasThePoint(First));

        int hasTheSecond = Convert.ToInt32(triangle.HasThePoint(Second));

        int hasTheThird = Convert.ToInt32(triangle.HasThePoint(Third));

        return (TriangleRelation)(hasTheFirst * (int)TriangleRelation.MeetAtFirst +
                                    hasTheSecond * (int)TriangleRelation.MeetAtSecond +
                                    hasTheThird * (int)TriangleRelation.MeetAtThird);
    }

    public bool HasTheEdge(QuasiEdge edge)
    {
        return HasThePoint(edge.First) && HasThePoint(edge.Second);
    }

    public int GetThirdPoint(QuasiEdge edge)
    {
        if (!HasTheEdge(edge))
        {
            throw new NotImplementedException();
        }

        if (first != edge.First && first != edge.Second)
        {
            return first;
        }
        else if (Second != edge.First && Second != edge.Second)
        {
            return Second;
        }
        else if (third != edge.First && third != edge.Second)
        {
            return third;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public int GetNeighbour(QuasiEdge commonEdge)
    {
        if (!HasTheEdge(commonEdge))
        {
            throw new NotImplementedException();
        }

        if (first == commonEdge.First)
        {
            if (second == commonEdge.Second)
            {
                return FirstSecondNeighbour;
            }
            else if (third == commonEdge.Second)
            {
                return ThirdFirstNeighbour;
            }
        }
        else if (second == commonEdge.First)
        {
            if (first == commonEdge.Second)
            {
                return FirstSecondNeighbour;
            }
            else if (third == commonEdge.Second)
            {
                return SecondThirdNeighbour;
            }
        }
        else if (third == commonEdge.First)
        {
            if (first == commonEdge.Second)
            {
                return ThirdFirstNeighbour;
            }
            else if (second == commonEdge.Second)
            {
                return SecondThirdNeighbour;
            }
        }

        throw new NotImplementedException();
    }


}