// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Spatial.Primitives;

public class Triangle
{
    private Point firstPoint, secondPoint, thirdPoint;

    private int firstSecondNeighbour, secondThirdNeighbour, thirdFirstNeighbour;

    //directed arrow from first to second edge
    public static readonly int firstEdgeWeight = 1;

    //directed arrow from second to third edge
    public static readonly int secondEdgeWeight = 3;

    //directed arrow from third to first edge
    public static readonly int thirdEdgeWeight = 9;

    public Point FirstPoint
    {
        get { return firstPoint; }
        set { firstPoint = value; }
    }

    public Point SecondPoint
    {
        get { return secondPoint; }
        set { secondPoint = value; }
    }

    public Point ThirdPoint
    {
        get { return thirdPoint; }
        set { thirdPoint = value; }
    }

    public double FirstSecondSide
    {
        get
        {
            return FirstPoint.DistanceTo(SecondPoint);
            //return ComputationalGeometry.CalculateDistance(FirstPoint, SecondPoint);
        }
    }

    public double SecondThirdSide
    {
        get
        {
            return SecondPoint.DistanceTo(ThirdPoint);
            //return ComputationalGeometry.CalculateDistance(SecondPoint, ThirdPoint);
        }
    }

    public double ThirdFirstSide
    {
        get
        {
            return ThirdPoint.DistanceTo(FirstPoint);
            //return ComputationalGeometry.CalculateDistance(ThirdPoint, FirstPoint);
        }
    }

    /// <summary>
    /// Angle in radian
    /// </summary>
    public double FirstAngle
    {
        get
        {
            double tempValue = (SecondThirdSide * SecondThirdSide - FirstSecondSide * FirstSecondSide - ThirdFirstSide * ThirdFirstSide)
                                /
                                (2 * FirstSecondSide * ThirdFirstSide);

            return Math.Acos(tempValue);
        }
    }

    /// <summary>
    /// Angle in radian
    /// </summary>
    public double SecondAngle
    {
        get
        {
            double tempValue = (ThirdFirstSide * ThirdFirstSide - FirstSecondSide * FirstSecondSide - SecondThirdSide * SecondThirdSide)
                                /
                                (2 * FirstSecondSide * SecondThirdSide);

            return Math.Acos(tempValue);
        }
    }

    /// <summary>
    /// Angle in radian
    /// </summary>
    public double ThirdAngle
    {
        get
        {
            double tempValue = (FirstSecondSide * FirstSecondSide - SecondThirdSide * SecondThirdSide - ThirdFirstSide * ThirdFirstSide)
                                /
                                (2 * SecondThirdSide * ThirdFirstSide);

            return Math.Acos(tempValue);
        }
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

    public Triangle()
    {
        firstSecondNeighbour = -1;

        secondThirdNeighbour = -1;

        thirdFirstNeighbour = -1;
    }

    public Triangle(Point first, Point second, Point third)
    {
        firstPoint = first;

        secondPoint = second;

        thirdPoint = third;

        firstSecondNeighbour = -1;

        secondThirdNeighbour = -1;

        thirdFirstNeighbour = -1;
    }

    public override string ToString()
    {
        return string.Format("First:{0}, Second:{1}, Third:{2}", firstPoint.ToString(), secondPoint.ToString(), thirdPoint.ToString());
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(Triangle))
        {
            Triangle tempTriangle = (Triangle)obj;

            return tempTriangle.FirstPoint.Equals(FirstPoint) &&
                    tempTriangle.SecondPoint.Equals(SecondPoint) &&
                    tempTriangle.ThirdPoint.Equals(ThirdPoint);
        }

        return false;
    }

    public double CalculateArea()
    {
        double firstSecondLength = FirstPoint.DistanceTo(SecondPoint);

        double secondThirdLength = SecondPoint.DistanceTo(ThirdPoint);

        double thirdFirstLength = ThirdPoint.DistanceTo(FirstPoint);

        double semiPerimeter = (firstSecondLength + secondThirdLength + thirdFirstLength) / 2;

        return Math.Sqrt(semiPerimeter *
                        (semiPerimeter - firstSecondLength) *
                        (semiPerimeter - secondThirdLength) *
                        (semiPerimeter - thirdFirstLength));
    }

    public double CalculatePerimeter()
    {
        //double firstSecondLength = this.FirstPoint.CalculateDistance(this.SecondPoint);

        //double secondThirdLength = this.SecondPoint.CalculateDistance(this.ThirdPoint);

        //double thirdFirstLength = this.ThirdPoint.CalculateDistance(this.FirstPoint);

        return FirstSecondSide + SecondThirdSide + ThirdFirstSide;
    }

    //public bool HasThePoint(int pointCode)
    //{
    //    return this.first == pointCode ||
    //            this.second == pointCode ||
    //            this.third == pointCode;
    //}

    //public TriangleRelation GetRelationTo(QuasiTriangle triangle)
    //{
    //    int hasTheFirst = System.Convert.ToInt32(triangle.HasThePoint(this.First));

    //    int hasTheSecond = System.Convert.ToInt32(triangle.HasThePoint(this.Second));

    //    int hasTheThird = System.Convert.ToInt32(triangle.HasThePoint(this.Third));

    //    return (TriangleRelation)((hasTheFirst * (int)TriangleRelation.MeetAtFirst) +
    //                                hasTheSecond * (int)TriangleRelation.MeetAtSecond +
    //                                hasTheThird * (int)TriangleRelation.MeetAtThird);
    //}

    //public bool HasTheEdge(QuasiEdge edge)
    //{
    //    return (this.HasThePoint(edge.First) && this.HasThePoint(edge.Second));
    //}

    //public int GetThirdPoint(QuasiEdge edge)
    //{
    //    if (!HasTheEdge(edge))
    //    {
    //        throw new NotImplementedException();
    //    }

    //    if (this.first != edge.First && this.first != edge.Second)
    //    {
    //        return this.first;
    //    }
    //    else if (this.Second != edge.First && this.Second != edge.Second)
    //    {
    //        return this.Second;
    //    }
    //    else if (this.third != edge.First && this.third != edge.Second)
    //    {
    //        return this.third;
    //    }
    //    else
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public int GetNeighbour(QuasiEdge commonEdge)
    //{
    //    if (!HasTheEdge(commonEdge))
    //    {
    //        throw new NotImplementedException();
    //    }

    //    if (this.first == commonEdge.First)
    //    {
    //        if (this.second == commonEdge.Second)
    //        {
    //            return this.FirstSecondNeighbour;
    //        }
    //        else if (this.third == commonEdge.Second)
    //        {
    //            return this.ThirdFirstNeighbour;
    //        }
    //    }
    //    else if (this.second == commonEdge.First)
    //    {
    //        if (this.first == commonEdge.Second)
    //        {
    //            return this.FirstSecondNeighbour;
    //        }
    //        else if (this.third == commonEdge.Second)
    //        {
    //            return this.SecondThirdNeighbour;
    //        }
    //    }
    //    else if (this.third == commonEdge.First)
    //    {
    //        if (this.first == commonEdge.Second)
    //        {
    //            return this.ThirdFirstNeighbour;
    //        }
    //        else if (this.second == commonEdge.Second)
    //        {
    //            return this.SecondThirdNeighbour;
    //        }
    //    }

    //    throw new NotImplementedException();
    //}

    /// <summary>
    /// Calculate the intersection of the triangle's medians
    /// </summary>
    /// <returns></returns>
    public Point CalculateCentroid()
    {
        double x = (FirstPoint.X + SecondPoint.X + ThirdPoint.X) / 3;

        double y = (FirstPoint.Y + SecondPoint.Y + ThirdPoint.Y) / 3;

        return new Point(x, y);
    }

    /// <summary>
    /// Calculate the intersection of perpendicular bisectors
    /// </summary>
    /// <returns></returns>
    public Point CalculateCircumcenter()
    {
        double x1 = FirstPoint.X; double y1 = FirstPoint.Y;

        double x2 = SecondPoint.X; double y2 = SecondPoint.Y;

        double x3 = ThirdPoint.X; double y3 = ThirdPoint.Y;

        double temp = 2 * (x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2));

        double tempX = ((y1 * y1 + x1 * x1) * (y2 - y3) + (y2 * y2 + x2 * x2) * (y3 - y1) + (y3 * y3 + x3 * x3) * (y1 - y2)) / temp;

        double tempY = ((y1 * y1 + x1 * x1) * (x3 - x2) + (y2 * y2 + x2 * x2) * (x1 - x3) + (y3 * y3 + x3 * x3) * (x2 - x1)) / temp;

        return new Point(tempX, tempY);
    }

    /// <summary>
    /// Calculate the intersection of triangle's bisectors
    /// </summary>
    /// <returns></returns>
    public Point CalculateIncenter()
    {
        double a = SecondThirdSide;

        double b = ThirdFirstSide;

        double c = FirstSecondSide;

        double perimeter = a + b + c;

        return new Point((a * FirstPoint.X + b * SecondPoint.X + c * ThirdPoint.X) / perimeter,
                            (a * FirstPoint.Y + b * SecondPoint.Y + c * ThirdPoint.Y) / perimeter);
    }
    //((a2 + b2 − c2)(a2  − b2 + c2):(a2 + b2  − c2)( − a2 + b2 + c2):(a2 − b2 + c2)( − a2 + b2 + c2)).

    //public Point CalculateOrthocenter()
    //{
    //     double aPrime = this.SecondThirdSide*this.SecondThirdSide;

    //    double bPrime = this.ThirdFirstSide* this.ThirdFirstSide;

    //    double cPrime = this.FirstSecondSide*this.FirstSecondSide;



    //}
}

