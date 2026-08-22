using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Spatial.Analysis.SFC;

public struct SpaceFillingCurve
{
    private int baseSize;

    List<Point> orderedPoints;

    private List<Move> bMFs;

    private List<Transform> mTFs;

    public SpaceFillingCurve(int baseSize, List<Move> movements, List<Transform> subLevelTransforms)
    {
        if (movements.Count + 1 != subLevelTransforms.Count)
        {
            throw new NotImplementedException();
        }

        if (baseSize * baseSize != subLevelTransforms.Count)
        {
            throw new NotImplementedException();
        }

        this.baseSize = baseSize;

        bMFs = movements;

        mTFs = subLevelTransforms;

        //
        bMFs.Add(bMFs[bMFs.Count - 1]);
        //

        orderedPoints = new List<Point>();

        Point tempPoint = GetStartPointRelativeLocation();

        orderedPoints.Add(tempPoint);

        for (int i = 0; i < NumberOfSteps - 1; i++)
        {
            tempPoint = this[i](tempPoint, 1);

            orderedPoints.Add(tempPoint);
        }
    }

    public Move this[int index]
    {
        get { return bMFs[index]; }
    }

    public int NumberOfSteps
    {
        get { return mTFs.Count; }
    }

    public int BaseSize
    {
        get { return baseSize; }
    }

    public List<Move> GetMoves()
    {
        return bMFs;
    }

    public int GetSubRegionIndex(Point point)
    {
        return orderedPoints.IndexOf(point);
    }

    public SpaceFillingCurve CalculateSubregionBMFs(int transformInex)
    {
        List<Move> newBMFs = new List<Move>();

        for (int i = 0; i < NumberOfSteps - 1; i++)
        {
            //97.10.11: slow approach but functional
            //newBMFs.Add(mTFs[transformInex](this.bMFs[i]));

            //97.10.12: fast approach but not using FoG
            var displacement = mTFs[transformInex](bMFs[i])(new Point(0, 0), 1);
            newBMFs.Add((p, step) => new Point(p.X + step * displacement.X, p.Y + step * displacement.Y));
        }

        return new SpaceFillingCurve(BaseSize, newBMFs, mTFs);
    }

    public List<Point> TraverseTheBasePath(Point current)
    {
        List<Point> result = new List<Point>();

        Point temp = current;

        result.Add(temp);

        for (int i = 0; i < NumberOfSteps - 1; i++)
        {
            temp = this[i](temp, 1);

            result.Add(temp);
        }

        return result;
    }

    public List<Point> TraverseTheBasePath(Point current, int step)
    {
        List<Point> result = new List<Point>();

        Point temp = current;

        result.Add(temp);

        for (int i = 0; i < NumberOfSteps - 1; i++)
        {
            temp = this[i](temp, step);

            result.Add(temp);
        }

        return result;
    }

    public Point GetStartPointRelativeLocation()
    {
        Point point = new Point(0, 0);

        double deltaX = 0, deltaY = 0;

        for (int i = 0; i < NumberOfSteps - 1; i++)
        {
            point = this[i](point, 1);

            deltaX = Math.Min(point.X, deltaX);

            deltaY = Math.Min(point.Y, deltaY);
        }

        return new Point(-deltaX, -deltaY);
    }

    public Point GetStartPointRelativeLocation(int subregionSize)
    {
        List<Point> relativePoints = new List<Point>();

        relativePoints.Add(GetStartPointRelativeLocation());

        SpaceFillingCurve temp = this;

        while (subregionSize > baseSize)
        {
            subregionSize = subregionSize / baseSize;

            temp = temp.CalculateSubregionBMFs(0);

            relativePoints.Add(temp.GetStartPointRelativeLocation());
        }

        Point result = new Point(0, 0);

        for (int i = 0; i < relativePoints.Count; i++)
        {
            result.X = result.X + relativePoints[i].X * Math.Pow(baseSize, relativePoints.Count - 1 - i);

            result.Y = result.Y + relativePoints[i].Y * Math.Pow(baseSize, relativePoints.Count - 1 - i);
        }

        return result;
    }

    public Point MoveToNextRegion(int startIndex, Point startPoint, int subRegionSize)
    {
        SpaceFillingCurve sFC = this;

        Point firstRegionStartPoint = sFC.CalculateSubregionBMFs(startIndex).GetStartPointRelativeLocation(subRegionSize);

        Point secondRegionStartPoint = sFC.CalculateSubregionBMFs(startIndex + 1).GetStartPointRelativeLocation(subRegionSize);

        Move jumpFunc = (p, step) => new Point(p.X + (secondRegionStartPoint.X - firstRegionStartPoint.X),
                                                    p.Y + (secondRegionStartPoint.Y - firstRegionStartPoint.Y));

        return jumpFunc(sFC[startIndex](startPoint, subRegionSize), 1);

    }

    public int ComparePoints(Point first, Point second)
    {
        int firstIndex = orderedPoints.IndexOf(first);

        int secondIndex = orderedPoints.IndexOf(second);

        if (firstIndex < 0 || secondIndex < 0)
        {
            throw new NotImplementedException();
        }

        return firstIndex.CompareTo(secondIndex);
    }

    public int ComparePoints(Point first, Point second, Boundary mbb)
    {
        if (first.Equals(second))
        {
            return 0;
        }

        double tempUnitSize = (double)Math.Max(mbb.Height, mbb.Width);

        double deltaX = (tempUnitSize - mbb.Width) / 2.0; double deltaY = (tempUnitSize - mbb.Height) / 2.0;

        int firstX = (int)Math.Floor(Math.Round((first.X - mbb.MinX + deltaX) / (tempUnitSize / BaseSize), 13));

        int secondX = (int)Math.Floor(Math.Round((second.X - mbb.MinX + deltaX) / (tempUnitSize / BaseSize), 13));

        int firstY = (int)Math.Floor(Math.Round((first.Y - mbb.MinY + deltaY) / (tempUnitSize / BaseSize), 13));

        int secondY = (int)Math.Floor(Math.Round((second.Y - mbb.MinY + deltaY) / (tempUnitSize / BaseSize), 13));

        if (firstX.Equals(double.NaN))
        {
            throw new NotImplementedException();
        }

        //if (firstX == this.BaseSize)
        //{
        //    firstX--;
        //}

        //if (secondX == this.BaseSize)
        //{
        //    secondX--;
        //}

        //if (firstY == this.BaseSize)
        //{
        //    firstY--;
        //}

        //if (secondY == this.BaseSize)
        //{
        //    secondY--;
        //}

        if (firstX == secondX && firstY == secondY)
        {
            int index = GetSubRegionIndex(new Point(firstX, firstY));

            SpaceFillingCurve newSFC = CalculateSubregionBMFs(index);

            Boundary newMbb = mbb.GetSubboundary(firstX, firstY, tempUnitSize / BaseSize, tempUnitSize / BaseSize);

            newMbb.MoveBoundary(-deltaX, -deltaY);

            if (!newMbb.DoseContainsPoint(first) || !newMbb.DoseContainsPoint(second))
            {
                throw new NotImplementedException();
            }

            return newSFC.ComparePoints(first, second, newMbb);
        }
        else
        {
            return ComparePoints(new Point(firstX, firstY), new Point(secondX, secondY));
        }
    }


    public static List<Point> ConstructRasterSFC(int height, int width, SpaceFillingCurve sFC)
    {
        Point startPoint = sFC.GetStartPointRelativeLocation(width);

        return ConstructRasterSFC(startPoint, height, width, sFC);
    }

    private static List<Point> ConstructRasterSFC(Point startPoint, int height, int width, SpaceFillingCurve sFC)
    {
        List<Point> result = new List<Point>();

        if (height * width == sFC.NumberOfSteps)
        {
            return sFC.TraverseTheBasePath(startPoint);
        }

        int newHeight = height / sFC.BaseSize;

        int newWidth = width / sFC.BaseSize;

        Point tempPoint = startPoint;

        for (int i = 0; i < sFC.NumberOfSteps; i++)
        {
            SpaceFillingCurve temp = sFC.CalculateSubregionBMFs(i);

            result.AddRange(ConstructRasterSFC(tempPoint, newHeight, newWidth, temp));

            if (i == sFC.NumberOfSteps - 1)
                continue;

            tempPoint = sFC.MoveToNextRegion(i, tempPoint, newWidth);
        }

        return result;
    }

    public static List<double> Project2DDataTo1D(double[,] values, SpaceFillingCurve sFC)
    {
        int width = values.GetLength(0);

        int height = values.GetLength(1);

        Point startPoint = sFC.GetStartPointRelativeLocation(width);

        return Project2DDataTo1D(startPoint, height, width, values, sFC);
    }

    private static List<double> Project2DDataTo1D(Point startPoint, int height, int width, double[,] values, SpaceFillingCurve sFC)
    {
        List<double> result = new List<double>();

        if (height * width == sFC.NumberOfSteps)
        {
            List<Point> temp = sFC.TraverseTheBasePath(startPoint);

            List<double> tempResult = new List<double>();

            foreach (Point item in temp)
            {
                tempResult.Add(values[(int)item.X, (int)item.Y]);
            }

            return tempResult;
        }

        int newHeight = height / sFC.BaseSize;

        int newWidth = width / sFC.BaseSize;

        Point tempPoint = startPoint;

        for (int i = 0; i < sFC.NumberOfSteps; i++)
        {
            SpaceFillingCurve temp = sFC.CalculateSubregionBMFs(i);

            result.AddRange(Project2DDataTo1D(tempPoint, newHeight, newWidth, values, temp));

            if (i == sFC.NumberOfSteps - 1)
                continue;

            tempPoint = sFC.MoveToNextRegion(i, tempPoint, newWidth);
        }

        return result;
    }

    public static Point TransformIndexToPoint(SpaceFillingCurve sFC, int index, int size)
    {
        List<int> baseRepresentation = new List<int>();

        int baseValue = sFC.NumberOfSteps;

        int outputLength = (int)Math.Log(size, sFC.BaseSize);

        int temp = index;

        do
        {
            baseRepresentation.Add(temp % baseValue);

            temp = temp / baseValue;

        } while (temp >= 1 || baseRepresentation.Count < outputLength);

        baseRepresentation.Reverse();

        Point startPoint = sFC.GetStartPointRelativeLocation();

        startPoint = new Point(startPoint.X * (size - 1) / (sFC.BaseSize - 1), startPoint.Y * (size - 1) / (sFC.BaseSize - 1));

        for (int i = 0; i < baseRepresentation.Count; i++)
        {
            size = size / sFC.BaseSize;

            if (size == 1)
            {
                for (int j = 0; j < baseRepresentation[i]; j++)
                {
                    startPoint = sFC[j](startPoint, 1);
                }
            }
            else
            {
                for (int j = 0; j < baseRepresentation[i]; j++)
                {
                    startPoint = sFC.MoveToNextRegion(j, startPoint, size);
                }
            }

            sFC = sFC.CalculateSubregionBMFs(baseRepresentation[i]);
        }

        return startPoint;
    }

    public static int TransformPointToIndex(SpaceFillingCurve sFC, Point point, int size)
    {
        List<Point> temp = new List<Point>();

        int level = (int)Math.Log(size, sFC.BaseSize);

        while (temp.Count < level)
        {
            temp.Add(new Point(point.X % sFC.BaseSize, point.Y % sFC.BaseSize));

            point = new Point((int)point.X / sFC.BaseSize, (int)point.Y / sFC.BaseSize);
        }

        temp.Reverse();

        List<int> indexes = new List<int>();

        for (int i = 0; i < temp.Count; i++)
        {
            int index = sFC.GetSubRegionIndex(temp[i]);

            indexes.Add(index);

            sFC = sFC.CalculateSubregionBMFs(index);
        }

        indexes.Reverse();

        int result = 0;

        for (int i = 0; i < indexes.Count; i++)
        {
            result += indexes[i] * (int)Math.Pow(sFC.NumberOfSteps, i);
        }

        return result;
    }

    public static double[,] MemoryEfficientOverlay(List<double> firstValues, SpaceFillingCurve firstSfc,
                                        List<double> secondValues, SpaceFillingCurve secondSfc,
                                        int height, int width,
                                        Func<double, double, double> func)
    {
        if (height * width != firstValues.Count || height * width != secondValues.Count)
        {
            throw new NotImplementedException();
        }

        double[,] result = new double[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int firstIndex = TransformPointToIndex(firstSfc, new Point(i, j), height);

                int secondIndex = TransformPointToIndex(firstSfc, new Point(i, j), height);

                result[i, j] = func(firstValues[firstIndex], secondValues[secondIndex]);
            }
        }

        return result;
    }

    public static double[,] SpeedEfficientOverlay(List<double> firstValues, SpaceFillingCurve firstSfc,
                                        List<double> secondValues, SpaceFillingCurve secondSfc,
                                        int height, int width,
                                        Func<double, double, double> func)
    {
        if (height * width != firstValues.Count || height * width != secondValues.Count)
        {
            throw new NotImplementedException();
        }

        List<Point> first = ConstructRasterSFC(height, width, firstSfc);

        List<Point> second = ConstructRasterSFC(height, width, secondSfc);

        double[,] firstGrid = new double[height, width];

        double[,] secondGrid = new double[height, width];

        for (int i = 0; i < height * width; i++)
        {
            firstGrid[(int)first[i].X, (int)first[i].Y] = firstValues[i];
        }

        for (int i = 0; i < height * width; i++)
        {
            secondGrid[(int)second[i].X, (int)second[i].Y] = secondValues[i];
        }


        double[,] result = new double[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                result[i, j] = func(firstGrid[i, j], secondGrid[i, j]);
            }
        }

        return result;
    }
}

public delegate List<Transform> firstToOtherSubregionTransforms(List<Move> bMFs);
