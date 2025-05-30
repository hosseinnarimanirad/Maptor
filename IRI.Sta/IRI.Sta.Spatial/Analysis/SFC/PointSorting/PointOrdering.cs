﻿using IRI.Sta.Common.Primitives;
using IRI.Sta.DataStructures;
using IRI.Sta.Spatial.Primitives;

namespace IRI.Sta.Spatial.Analysis.SFC;

public class PointOrdering
{

    public static int HilbertComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Hilbert(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }

    public static int HosseinComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Hossein(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }

    public static int NOrderingComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.NOrdering(Moves.North, Moves.SouthEast).ComparePoints(first, second, mbb);
    }

    public static int GrayComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Gray(Moves.North, Moves.East, Moves.South).ComparePoints(first, second, mbb);
    }

    public static int MooreComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Moore(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }

    public static int ZOrderingComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.NOrdering(Moves.East, Moves.SouthWest).ComparePoints(first, second, mbb);
    }

    public static int DiagonalLebesgueComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.LebesgueDiagonal(Moves.NorthEast, Moves.West).ComparePoints(first, second, mbb);
    }

    public static int UOrderOrLebesgueSquareComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.UOrderOrLebesgueSquare(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }

    public static int PeanoComparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Peano(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }

    public static int Peano02Comparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Peano02(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }

    public static int Peano03Comparer(Point first, Point second, Boundary mbb)
    {
        return SpaceFillingCurves.Peano03(Moves.North, Moves.East).ComparePoints(first, second, mbb);
    }


    public static Point[] HilbertSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 0.0005);

        var sfc = SpaceFillingCurves.Hilbert(Moves.North, Moves.East);

        //IRI.Sta.DataStructures.SortAlgorithm.QuickSort<Point>(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
        //return array;

        //Array.Sort(array, new Comparison<Point>((p1, p2) => sfc.ComparePoints(p1, p2, boundary)));
        //return array;

        //return IRI.Sta.DataStructures.SortAlgorithm.Heapsort<Point>(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
        //return IRI.Sta.DataStructures.SortAlgorithm.MergeSort<Point>(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));


        SortAlgorithm.BubbleSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
        return array;

    }

    public static Point[] HosseinSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        var sfc = SpaceFillingCurves.Hossein(Moves.North, Moves.East);

        return SortAlgorithm.MergeSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
    }

    public static Point[] NOrderingSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        var sfc = SpaceFillingCurves.NOrdering(Moves.North, Moves.SouthEast);

        //return SortAlgorithm.Heapsort<Point>(array, (p1, p2) => NOrderingComparer(p1, p2, boundary));
        //return SortAlgorithm.MergeSort<Point>(array, (p1, p2) => NOrderingComparer(p1, p2, boundary));
        //IRI.Sta.DataStructures.SortAlgorithm.QuickSort<Point>(array, (p1, p2) => HilbertComparer(p1, p2, boundary));
        //return array;

        return SortAlgorithm.MergeSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
    }

    public static Point[] GraySorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        var sfc = SpaceFillingCurves.Gray(Moves.North, Moves.East, Moves.South);

        return SortAlgorithm.MergeSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
        //SortAlgorithm.QuickSort<Point>(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));

        //return array;
    }

    public static Point[] MooreSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        var sfc = SpaceFillingCurves.Moore(Moves.North, Moves.East);

        return SortAlgorithm.MergeSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
    }

    public static Point[] ZOrderingSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        var sfc = SpaceFillingCurves.ZOrdering();

        return SortAlgorithm.MergeSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
    }

    public static Point[] DiagonalLebesgueSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        return SortAlgorithm.MergeSort(array, (p1, p2) => DiagonalLebesgueComparer(p1, p2, boundary));
    }

    public static Point[] UOrderOrLebesgueSquareSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        return SortAlgorithm.MergeSort(array, (p1, p2) => UOrderOrLebesgueSquareComparer(p1, p2, boundary));
    }

    public static Point[] PeanoSorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        var sfc = SpaceFillingCurves.Peano(Moves.North, Moves.East);

        return SortAlgorithm.MergeSort(array, (p1, p2) => sfc.ComparePoints(p1, p2, boundary));
    }

    public static Point[] Peano02Sorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        return SortAlgorithm.MergeSort(array, (p1, p2) => Peano02Comparer(p1, p2, boundary));
    }

    public static Point[] Peano03Sorter(Point[] array)
    {
        Boundary boundary = GetBoundary(array, 5);

        return SortAlgorithm.MergeSort(array, (p1, p2) => Peano03Comparer(p1, p2, boundary));
    }




    public static Boundary GetBoundary(Point[] array, double expandFactor)
    {
        double xMin = array[0].X;

        double xMax = array[0].X;

        double yMin = array[0].Y;

        double yMax = array[0].Y;

        for (int i = 1; i < array.Length; i++)
        {
            if (array[i].X < xMin)
            {
                xMin = array[i].X;
            }
            if (array[i].Y < yMin)
            {
                yMin = array[i].Y;
            }
            if (array[i].X > xMax)
            {
                xMax = array[i].X;
            }
            if (array[i].Y > yMax)
            {
                yMax = array[i].Y;
            }
        }

        return new Boundary(new Point(xMin - expandFactor, yMin - expandFactor), new Point(xMax + expandFactor, yMax + expandFactor));
    }

    public static Boundary GetBoundary(Point[] array, int expandFactor)
    {
        double xMin = array[0].X;

        double xMax = array[0].X;

        double yMin = array[0].Y;

        double yMax = array[0].Y;

        for (int i = 1; i < array.Length; i++)
        {
            if (array[i].X < xMin)
            {
                xMin = array[i].X;
            }
            if (array[i].Y < yMin)
            {
                yMin = array[i].Y;
            }
            if (array[i].X > xMax)
            {
                xMax = array[i].X;
            }
            if (array[i].Y > yMax)
            {
                yMax = array[i].Y;
            }
        }

        return new Boundary(new Point(xMin - expandFactor, yMin - expandFactor), new Point(xMax + expandFactor, yMax + expandFactor));
    }
}
