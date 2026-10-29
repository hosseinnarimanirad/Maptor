using IRI.Maptor.Extensions;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Common.Helpers;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Analysis;

public static class Simplifications
{
    public static List<T> SimplifyByAdditiveAreaPlus<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        List<int> filtered = new List<int>();

        filtered.Add(0);

        int firstIndex = 0, secondIndex = 1, thirdIndex = 2;

        var totalAreaCoef = SpatialUtility.IsClockwise(pointList) ? 1 : -1;

        double tempArea = 0;

        while (thirdIndex < pointList.Count)
        {
            var area = SpatialUtility.GetSignedEuclideanArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            var areaCheck2 = SpatialUtility.GetUnsignedEuclideanArea(pointList[firstIndex], pointList[(int)((firstIndex + thirdIndex) / 2.0)], pointList[thirdIndex]);

            tempArea += area;

            if (Math.Abs(tempArea) > parameters.AreaThreshold || areaCheck2 > parameters.AreaThreshold || (area * totalAreaCoef > 0))
            {
                tempArea = 0;

                filtered.Add(secondIndex);

                firstIndex = secondIndex;
            }

            secondIndex = thirdIndex;

            thirdIndex = thirdIndex + 1;
        }

        if (filtered.Count == 1)
        {
            filtered.Add(pointList.Count() / 2);
        }

        filtered.Add(pointList.Count - 1);

        var result = filtered.Select(i => pointList[i]).ToList();

        return result;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointList"></param>
    /// <param name="threshold">Must be between 0 and 1</param>
    /// <returns></returns>
    public static List<T> SimplifyByCumulativeAngle<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        List<int> filtered = new List<int>();

        filtered.Add(0);

        int firstIndex = 0, secondIndex = 1, thirdIndex = 2;

        while (thirdIndex < pointList.Count)
        {
            var angle = SpatialUtility.SignedSquareCosineOfTurn(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            if (angle < 0 || angle < parameters.AngleThreshold.Value)
            {
                filtered.Add(secondIndex);

                firstIndex = secondIndex;
            }

            secondIndex = thirdIndex;

            thirdIndex = thirdIndex + 1;
        }

        if (parameters.Retain3Points && filtered.Count == 1)
        {
            filtered.Add(pointList.Count() / 2);
        }

        filtered.Add(pointList.Count - 1);

        var result = filtered.Select(i => pointList[i]).ToList();

        return result;
    }

    /// <summary>
    /// Additive Angle + Area Check
    /// </summary>
    /// <param name="pointList"></param>
    /// <param name="anglethreshold">Must be between 0 and 1</param>
    /// <returns></returns>
    public static List<T> SimplifyByCumulativeAngleArea<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        List<int> filtered = new List<int>();

        filtered.Add(0);

        int firstIndex = 0, secondIndex = 1, thirdIndex = 2;

        double tempArea = 0;

        while (thirdIndex < pointList.Count)
        {
            var angle = SpatialUtility.SignedSquareCosineOfTurn(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            var area = SpatialUtility.GetUnsignedEuclideanArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            tempArea += area;


            //if (Math.Abs(angle) < threshold)
            //{
            if ((angle < 0 || angle < parameters.AngleThreshold.Value) || tempArea > parameters.AreaThreshold.Value)
            {
                filtered.Add(secondIndex);

                firstIndex = secondIndex;

                tempArea = 0;
            }


            secondIndex = thirdIndex;

            thirdIndex = thirdIndex + 1;
        }

        if (parameters.Retain3Points && filtered.Count == 1)
        {
            filtered.Add(pointList.Count() / 2);
        }

        filtered.Add(pointList.Count - 1);

        var result = filtered.Select(i => pointList[i]).ToList();

        return result;
    }

    public static List<T> SimplifyByCumulativeEuclideanDistance<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        List<int> filtered = new List<int> { 0 };

        int secondIndex = 1;

        // length of the path walked since the last retained point; until 1405.07.14 (2026-10-06) the squared
        // distance to the last retained point was summed instead and compared with the (linear) distance threshold
        var temp = 0.0;

        while (secondIndex < pointList.Count - 1)
        {
            temp += SpatialUtility.GetEuclideanLength(pointList[secondIndex - 1], pointList[secondIndex]);

            if (temp > parameters.DistanceThreshold.Value)
            {
                temp = 0;

                filtered.Add(secondIndex);
            }

            secondIndex++;
        }

        if (parameters.Retain3Points && filtered.Count == 1)
        {
            filtered.Add(pointList.Count() / 2);
        }

        filtered.Add(pointList.Count - 1);

        var result = filtered.Select(i => pointList[i]).ToList();

        return result;
    }



    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Tobler, W. R. (1966). Numerical map generalization: Department of Geography, University of
    //      Michigan Ann Arbour, MI, USA
    public static List<T> SimplifyByAngle<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        var angles = new List<(int index, double angle)>();

        for (int i = 0; i < pointList.Count - 2; i++)
        {
            var angle = SpatialUtility.SignedSquareCosineOfTurn(pointList[i], pointList[i + 1], pointList[i + 2]);

            angles.Add((index: i + 1, angle: angle));
        }

        //When i.Item2 ~ 1 it means points are on a line
        var filter1 = angles.Where(i => i.angle < parameters.AngleThreshold.Value).Select(i => i.index).ToList();

        filter1.Insert(0, 0);

        filter1.Add(pointList.Count - 1);

        var result = filter1.Select(i => pointList[i]).ToList();

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Tobler, W. R. (1966). Numerical map generalization: Department of Geography, University of
    //      Michigan Ann Arbour, MI, USA
    public static List<T> SimplifyByEuclideanDistance<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        var numberOfPoints = pointList.Count;

        result.Add(pointList[0]);

        int firstIndex = 0;

        for (int i = 1; i < numberOfPoints - 1; i++)
        {
            if (SpatialUtility.GetEuclideanLength(pointList[i], pointList[firstIndex]) > parameters.DistanceThreshold)
            {
                result.Add(pointList[i]);
                firstIndex = i;
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Tobler, W. R. (1966). Numerical map generalization: Department of Geography, University of
    //      Michigan Ann Arbour, MI, USA
    public static List<T> SimplifyByNthPoint<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        if (parameters.N == 0)
            throw new ArgumentOutOfRangeException(nameof(parameters), "SimplificationParameters.N must be at least 1 for the n-th point routine.");

        var numberOfPoints = pointList.Count;

        result.Add(pointList[0]);

        for (int i = 1; i < numberOfPoints; i++)
        {
            if (i % parameters.N == 0)
            {
                result.Add(pointList[i]);
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.06.24
    // ref: Tobler, W. R. (1966). Numerical map generalization: Department of Geography, University of
    //      Michigan Ann Arbour, MI, USA
    public static List<T> SimplifyByRandomPointSelection<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        if (parameters.N == 0)
            throw new ArgumentOutOfRangeException(nameof(parameters), "SimplificationParameters.N must be at least 1 for the random point routine.");

        var numberOfPoints = pointList.Count;

        result.Add(pointList[0]);

        // 1405.07.14 (2026-10-06)
        // "for every fixed number of consecutive points along the line, ... one random point among a set of these
        // consecutive points [is] retained" (Shi and Cheung 2006, p. 28): the interior points are split into sets of
        // N consecutive points (1..N, N+1..2N, ...) and one point of each set is kept; the n-th point routine keeps
        // the last point of each set instead. Before this date every point was kept with probability 0.5.
        var n = (int)parameters.N;

        for (int setStart = 1; setStart < numberOfPoints - 1; setStart += n)
        {
            var setEnd = Math.Min(setStart + n, numberOfPoints - 1); // exclusive

            result.Add(pointList[RandomHelper.Get(setStart, setEnd)]);
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Lang, T., 1969, Rules for robot draughtsmen. Geographical Magazine, vol.62, No.1, pp.50-51
    public static List<T> SimplifyByLang<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;


        // the default is local: writing it back into parameters made every later ring or part simplified with the
        // same parameters object inherit the look-ahead of the first one (fixed 1405.07.14, 2026-10-06)
        var lookAhead = parameters.LookAhead ?? Math.Max(3, pointList.Count / 10);

        var numberOfPoints = pointList.Count;


        // 1399.07.09
        // در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        // نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        // روش باعث می‌شود در محاسبات از تابع جذر استفاده
        // نشود
        //double effectiveThreshold = threshold * threshold;
        double distanceThreshold = parameters.DistanceThreshold.Value;

        int startIndex = 0;

        result.Add(pointList[0]);

        int endIndex = Math.Min(numberOfPoints - 1, lookAhead);

        while (true)
        {
            if (startIndex == endIndex)
            {
                break;
            }
            if (endIndex - startIndex == 1)
            {
                result.Add(pointList[endIndex]);
                startIndex = endIndex;
                endIndex = Math.Min(numberOfPoints - 1, endIndex + lookAhead);

                continue;
            }

            if (SpatialUtility.AnyPerpendicularDistanceExceedTolerance(pointList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList(), distanceThreshold))
            {
                endIndex--;
            }
            else
            {
                result.Add(pointList[endIndex]);
                startIndex = endIndex;
                endIndex = Math.Min(numberOfPoints - 1, endIndex + lookAhead);
            }
        }

        if (parameters.Retain3Points && result.Count == 2)
        {
            result.Insert(1, pointList[pointList.Count / 2]);
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Ramer, U., An iterative procedure for the polygonal approximation of plane curves.
    //      Computer graphics and image processing, 1972. 1(3): p. 244-256.
    // ref: Douglas, D.H.and T.K.Peucker, Algorithms for the reduction of the number of points
    //      required to represent a digitized line or its caricature. Cartographica: the international
    //      journal for geographic information and geovisualization, 1973. 10(2): p. 112-122
    // link: https://doi.org/10.3138/FM57-6770-U75U-7727
    public static List<T> SimplifyByRamerDouglasPeucker<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        if (parameters.IsRing)
            return SimplifyRingAsClosedLine(pointList, parameters, SimplifyByRamerDouglasPeucker);

        // 1405.07.14 (2026-10-06)
        // Closed line (first point = last point): "In the case of closed loops, where the first and the last point
        // do not define a line then the maximum perpendicular distance from the segment is replaced with the maximum
        // distance from the point" (Douglas and Peucker 1973, p. 117). GetPointToLineDistance returns exactly that
        // distance for a zero-length chord, so the loop below splits a closed line at the point farthest from its
        // start. Until this date a closed line was always split at its middle index (Count / 2), keeping that point
        // untested.

        var numberOfPoints = pointList.Count;

        //
        double maxPerpendicularDistance = 0;
        int maxIndex = 0;

        //1399.06.23
        //در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        //نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        //روش باعث می‌شود در محاسبات از تابع جزر استفاده
        //نشود
        //double effectiveThreshold = threshold * threshold;
        double effectiveThreshold = parameters.DistanceThreshold.Value;

        for (int i = 1; i < numberOfPoints - 1; i++)
        {
            var perpendicularDistance = SpatialUtility.GetPointToLineDistance(pointList[0], pointList[numberOfPoints - 1], pointList[i]);

            if (perpendicularDistance > maxPerpendicularDistance)
            {
                maxIndex = i;
                maxPerpendicularDistance = perpendicularDistance;
            }
        }

        if (maxPerpendicularDistance > effectiveThreshold)
        {
            return DivideForDouglasPeucker(pointList, parameters, maxIndex);
        }
        else
        {
            return new List<T> { pointList[0], pointList[numberOfPoints - 1] };
        }
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: K. Reumann and A.P.M. Witkam. Optimizing curve segmentation in computer graphics.
    //      In Proceedings of the International Computing Symposium, pages 467–472, 1974
    // link: http://psimpl.sourceforge.net/reumann-witkam.html
    public static List<T> SimplifyByReumannWitkam<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;


        var numberOfPoints = pointList.Count;

        //1399.06.23
        //در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        //نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        //روش باعث می‌شود در محاسبات از تابع جزر استفاده
        //نشود
        //double effectiveThreshold = threshold * threshold;
        double effectiveThreshold = parameters.DistanceThreshold.Value;

        result.Add(pointList[0]);

        int startIndex = 0;

        int middleIndex = 1;

        for (int i = 2; i < numberOfPoints; i++)
        {
            var temporaryDistance = SpatialUtility.GetPointToLineDistance(pointList[startIndex], pointList[middleIndex], pointList[i]);

            if (temporaryDistance > effectiveThreshold)
            {
                result.Add(pointList[i - 1]);
                startIndex = i - 1;
                middleIndex = i;
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1400.05.11
    // ref: Jenks, G. F. (1981). Lines, computers, and human frailties. Annals of the Association
    //      of American Geographers, 71(1), 1-10.
    // ref: Ekdemir, S., Efficient Implementation of Polyline Simplification for Large Datasets
    //      and Usability Evaluation. Master’s thesis, Uppsala University, Department of Information
    //      Technology, 2011
    // link: http://psimpl.sourceforge.net/perpendicular-distance.html
    public static List<T> SimplifyByPerpendicularDistance<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());


        // 1400.06.05
        // استفاده از مقدار توان دوم در مقایسه مشکل‌ساز
        // نخواهد بود چون در هر حال تابع توان دوم هم 
        // اکیدا صعودی است و توان دوم هیچ مقداری از توان
        // دوم مقدار کم‌تر از خودش، بیش‌تر نخواهد شد. اما
        // برای این‌که مقایسه سرعت درست انجام شود
        // استفاده از توان دوم متوقف شد.

        // 1400.05.11
        //در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        //نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        //روش باعث می‌شود در محاسبات از تابع جزر استفاده
        //نشود

        double distanceThreshold = parameters.DistanceThreshold.Value;

        for (int i = 0; i < pointList.Count - 2; i++)
        {
            var perpendicularDistance = SpatialUtility.GetPointToLineDistance(pointList[i], pointList[i + 2], pointList[i + 1]);

            if (perpendicularDistance > distanceThreshold)
                result.Add(pointList[i + 1]);

            // 1400.06.05
            // نقطه حذف شده به عنوان نقطه شروع استفاده نمی‌شود
            // این روش در بهترین حالت ۵۰ درصد فشرده سازی ایجاد 
            // می کند
            else
            {
                result.Add(pointList[i + 2]);
                // توی حلقه هم یه مرتبه دیگه به آی اضافه می‌شه
                // پس در این حالت آی به علاوه دو می‌شه
                i++;
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.06.24
    // Modified methods means the traverse pattern has been modified
    public static List<T> SimplifyByModifiedPerpendicularDistance<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());


        double distanceThreshold = parameters.DistanceThreshold.Value;

        int startIndex = 0;

        int endIndex = 2;

        while (endIndex < pointList.Count)
        {
            int midIndex = (int)Math.Ceiling((startIndex + endIndex) / 2.0);

            if (midIndex == startIndex || midIndex == endIndex)
            {
                throw new NotImplementedException();
            }

            // distance of the middle point to the line start-end (until 1405.07.14, 2026-10-06 the end point's distance to
            // the line start-middle was measured)
            var perpendicularDistance = SpatialUtility.GetPointToLineDistance(pointList[startIndex], pointList[endIndex], pointList[midIndex]);

            if (perpendicularDistance > distanceThreshold)
            {
                result.Add(pointList[midIndex]);
                startIndex = midIndex;
            }

            endIndex++;
        }

        //for (int i = 0; i < pointList.Count - 2; i++)
        //{
        //    int midIndex = (int)Math.Ceiling((startIndex + i + 2) / 2.0);

        //    if (midIndex == startIndex || midIndex == i + 2)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    var perpendicularDistance = SpatialUtility.GetPointToLineDistance(pointList[startIndex], pointList[midIndex/*i + 2*/], pointList[i + 1]);

        //    if (perpendicularDistance > distanceThreshold)
        //    {
        //        //result.Add(pointList[i + 1]);
        //        //startIndex = i + 1;

        //        result.Add(pointList[midIndex]);
        //        startIndex = midIndex;
        //    }
        //}

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Visvalingam, M. and Whyatt, J. D. (1993). ‘Line generalization by repeated elimination
    //      of points’, The Cartographic Journal, 30, 46–51
    // link: https://www.tandfonline.com/doi/abs/10.1179/000870493786962263
    public static List<T> SimplifyByVisvalingamWhyatt<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        var isRing = parameters.IsRing;

        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        var areas = SpatialUtility.GetPrimitiveAreas(pointList, isRing);

        if (isRing)
        {
            // we are not going to remove the first point
            // so we remove the corresponding area
            areas.RemoveAt(areas.Count - 1);
        }

        List<T> pList = pointList.ToList();

        // with Retain3Points the elimination stops when three points are left
        while (areas.Count > 0 && !(parameters.Retain3Points && pList.Count <= 3))
        {
            var minArea = Statistics.GetMin(areas);

            if (minArea > parameters.AreaThreshold)
                break;

            var pointCount = pList.Count;
            var areaCount = areas.Count;

            //remove min areas
            for (int i = areas.Count - 1; i >= 0; i--)
            {
                if (areas[i] == minArea)
                {
                    //if ( isRing || i > 0)
                    //{
                    //    // (i-1, i, i+2): i+2 is used instead of i+1 because i+1 is going to be removed
                    //    var firstIndex = i - 1 < 0 ? pointCount - 1 : i - 1;
                    //    areas[firstIndex] = SpatialUtility.GetUnsignedTriangleArea(pList[firstIndex], pList[i], pList[(i + 2) % pointCount]);
                    //}
                    //if (isRing || i < areas.Count - 1)
                    //{
                    //    // (i, i+2, i+3): i is used instead of i+1 because i+1 is going to be removed
                    //    areas[(i + 1) % areaCount] = SpatialUtility.GetUnsignedTriangleArea(pList[i], pList[(i + 2) % pointCount], pList[(i + 3) % pointCount]);
                    //}

                    if (i > 0)
                    {
                        // (i-1, i, i+2): i+2 is used instead of i+1 because i+1 is going to be removed
                        var firstIndex = i - 1 < 0 ? pointCount - 1 : i - 1;

                        //if (i > areas.Count - 1) { }
                        //if (i + 2 > pList.Count - 1) { }
                        //if (firstIndex > areas.Count - 1) { }

                        areas[firstIndex] = SpatialUtility.GetUnsignedEuclideanArea(pList[firstIndex], pList[i], pList[(i + 2) % pointCount]);
                    }

                    if (i < areas.Count - 1)
                    {
                        //if ((i + 1) % areaCount > areas.Count - 1) { }
                        //if ((i + 3) % pointCount > pList.Count - 1) { }

                        // (i, i+2, i+3): i is used instead of i+1 because i+1 is going to be removed
                        areas[(i + 1) % areaCount] = SpatialUtility.GetUnsignedEuclideanArea(pList[i], pList[(i + 2) % pointCount], pList[(i + 3) % pointCount]);
                    }

                    //pList.RemoveAt((i + 1) % pointCount);
                    pList.RemoveAt(i + 1);
                    areas.RemoveAt(i);

                    break;
                }

            }

        }


        // 1405.07.14 (2026-10-06)
        // An open line never loses its last point (it has no effective area), so nothing has to be appended.
        // A ring is stored without the closing vertex and its last stored vertex can be eliminated like any
        // other; the former "append pointList.Last() if missing" step put that eliminated vertex back at the
        // end of the ring (Visvalingam 2015 Koch island at T = 2: the square plus (1, 0)).
        return pList;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.01.03
    // ref: Zhao, Z. and A. Saalfeld. Linear-time sleeve-fitting polyline simplification algorithms.
    //      In. Proceedings of AutoCarto 13. pages 214–223, 1997
    public static List<T> SimplifyBySleeveFitting<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;


        var numberOfPoints = pointList.Count;

        //در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        //نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        //روش باعث می‌شود در محاسبات از تابع جزر استفاده
        //نشود
        //double effectiveThreshold = threshold * threshold;
        double effectiveThreshold = parameters.DistanceThreshold.Value;

        result.Add(pointList[0]);

        int startIndex = 0;

        for (int endIndex = 2; endIndex < numberOfPoints; endIndex++)
        {
            for (int middleIndex = endIndex - 1; middleIndex > startIndex; middleIndex--)
            {
                var temporaryDistance = SpatialUtility.GetPointToLineDistance(pointList[startIndex], pointList[endIndex], pointList[middleIndex]);

                if (temporaryDistance > effectiveThreshold)
                {
                    result.Add(pointList[endIndex - 1]);
                    startIndex = endIndex - 1;
                    break;
                }
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1400.05.20
    // ref: Meratnia, N., & Rolf, A. (2004, March). Spatiotemporal compression techniques for moving
    //      point objects. In International Conference on Extending Database Technology (pp. 765-782).
    //      Springer, Berlin, Heidelberg. 
    public static List<T> SimplifyByNormalOpeningWindow<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;


        result.Add(pointList.First());

        // 1400.05.20
        //در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        //نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        //روش باعث می‌شود در محاسبات از تابع جزر استفاده
        //نشود
        double effectiveThreshold = parameters.DistanceThreshold.Value;

        int startIndex = 0, middleIndex = 1, endIndex = 2;

        while (endIndex < pointList.Count)
        {
            while (middleIndex < endIndex)
            {
                var semiDistance = SpatialUtility.GetPointToLineDistance(pointList[startIndex], pointList[endIndex], pointList[middleIndex]);

                if (semiDistance > effectiveThreshold)
                {
                    result.Add(pointList[middleIndex]);

                    startIndex = middleIndex;
                    //middleIndex = startIndex + 1;

                    // after breaking it will increment by 1 6 lines below
                    endIndex = middleIndex;

                    break;
                }

                middleIndex++;
            }

            middleIndex = startIndex + 1;
            endIndex++;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1400.05.20
    // ref: Meratnia, N., & Rolf, A. (2004, March). Spatiotemporal compression techniques for moving
    //      point objects. In International Conference on Extending Database Technology (pp. 765-782).
    //      Springer, Berlin, Heidelberg. 
    public static List<T> SimplifyByBeforeOpeningWindow<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        // 1400.05.20
        //در این جا برای سرعت بیش‌تر مقدار فاصله استفاه 
        //نمی‌شود بلکه توان دوم آن استفاده می‌شود. این 
        //روش باعث می‌شود در محاسبات از تابع جزر استفاده
        //نشود
        double effectiveThreshold = parameters.DistanceThreshold.Value/* * parameters.DistanceThreshold.Value*/;

        int startIndex = 0, middleIndex = 1, endIndex = 2;

        while (endIndex < pointList.Count)
        {
            while (middleIndex < endIndex)
            {
                var semiDistance = SpatialUtility.GetPointToLineDistance(pointList[startIndex], pointList[endIndex], pointList[middleIndex]);

                if (semiDistance > effectiveThreshold)
                {
                    result.Add(pointList[endIndex - 1]);

                    startIndex = endIndex - 1;
                    //middleIndex = startIndex + 1;

                    // after breaking it will increment by 1 6 lines below
                    endIndex = middleIndex;

                    break;
                }

                middleIndex++;
            }

            middleIndex = startIndex + 1;
            endIndex++;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Ekdemir, S., Efficient Implementation of Polyline Simplification for Large Datasets
    //      and Usability Evaluation. Master’s thesis, Uppsala University, Department of Information
    //      Technology, 2011
    public static List<T> SimplifyByTriangleRoutine<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        List<T> result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        for (int i = 0; i < pointList.Count - 2; i++)
        {
            var area = SpatialUtility.GetUnsignedEuclideanArea(pointList[i], pointList[i + 1], pointList[i + 2]);

            if (area > parameters.AreaThreshold)
            {
                result.Add(pointList[i + 1]);
            }
            // 1401.06.24
            else
            {
                result.Add(pointList[i + 2]);
                // توی حلقه هم یه مرتبه دیگه به آی اضافه می‌شه
                // پس در این حالت آی به علاوه دو می‌شه
                i++;
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.06.24
    public static List<T> SimplifyByModifiedTriangleRoutine<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        List<T> result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        int startIndex = 0;

        int endIndex = 2;

        while (endIndex < pointList.Count)
        {
            int midIndex = (int)Math.Ceiling((startIndex + endIndex) / 2.0);

            if (midIndex == startIndex || midIndex == endIndex)
            {
                throw new NotImplementedException();
            }

            var area = SpatialUtility.GetUnsignedEuclideanArea(pointList[startIndex], pointList[midIndex/*i + 1*/], pointList[endIndex]);

            if (area > parameters.AreaThreshold)
            {
                result.Add(pointList[midIndex]);
                startIndex = midIndex;
            }

            endIndex++;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // Threshold values are summed in each iteration
    public static List<T> SimplifyByCumulativeTriangleRoutine<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        List<T> result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        int firstIndex = 0, secondIndex = 1, thirdIndex = 2;

        double tempArea = 0;

        while (thirdIndex < pointList.Count)
        {
            //1399.07.10
            //var area = SpatialUtility.CalculateSignedTriangleArea(points[firstIndex], points[secondIndex], points[thirdIndex]);
            //tempArea += area;
            //if (Math.Abs(tempArea) > threshold)

            var area = SpatialUtility.GetUnsignedEuclideanArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            tempArea += area;

            if (tempArea > parameters.AreaThreshold /*threshold*/)
            {
                tempArea = 0;

                result.Add(pointList[secondIndex]);

                firstIndex = secondIndex;
            }

            secondIndex = thirdIndex;

            thirdIndex = thirdIndex + 1;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }

    public static List<T> SimplifyByCumulativeTriangleRoutine2<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint
    {
        List<T> result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        int firstIndex = 0, secondIndex = 1, thirdIndex = 2;

        double tempArea = 0;

        while (thirdIndex < pointList.Count)
        {
            //1399.07.10
            //var area = SpatialUtility.CalculateSignedTriangleArea(points[firstIndex], points[secondIndex], points[thirdIndex]);
            //tempArea += area;
            //if (Math.Abs(tempArea) > threshold)

            var area = SpatialUtility.GetUnsignedEuclideanArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            tempArea += area;

            if (tempArea > parameters.AreaThreshold /*threshold*/)
            {
                tempArea = 0;

                result.Add(pointList[secondIndex]);
                result.Add(pointList[thirdIndex]);

                firstIndex = thirdIndex;
            }

            secondIndex++;

            thirdIndex++;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }

    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Kronenfeld, B. J., Stanislawski, L. V., Buttenfield, B. P., & Brockmeyer, T. (2020).
    // Simplification of polylines by segment collapse: Minimizing areal displacement while preserving area.
    // International Journal of Cartography, 6(1), 22-46.
    // link: https://www.tandfonline.com/doi/abs/10.1080/23729333.2019.1631535
    public static List<T> SimplifyByAPSC<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        List<T> result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 4)
            return pointList;

        result = ApscSimplification<T>.SimplifiedLine(pointList, parameters.AreaThreshold.Value);

        if (parameters.Retain3Points && result.Count == 2)
        {
            result.Insert(1, pointList[pointList.Count() / 2]);
        }

        return result;
    }


    #region Private Methods

    /// <summary>
    /// RDP measuring the distance of every vertex to the chord <em>segment</em> (clamped to its end points) instead of
    /// the infinite line through it, which <see cref="SimplifyByRamerDouglasPeucker{T}"/> uses as in the original papers.
    /// With the line distance a thin arm whose tip lies near the extension of the chord measures as close and is dropped;
    /// with the segment distance it is kept. Added 2026-10-05 for the thin-part ablation (E12); not used by any published run.
    /// </summary>
    public static List<T> SimplifyByRamerDouglasPeuckerSegment<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        if (parameters.IsRing)
            return SimplifyRingAsClosedLine(pointList, parameters, SimplifyByRamerDouglasPeuckerSegment);

        // closed line: split at the point farthest from the start (Douglas and Peucker 1973, p. 117); the segment
        // distance of a zero-length chord is the distance to its point (see SimplifyByRamerDouglasPeucker)

        var numberOfPoints = pointList.Count;

        double maxDistance = 0;
        int maxIndex = 0;

        for (int i = 1; i < numberOfPoints - 1; i++)
        {
            var distance = Helpers.TopologyUtility.GetPointToLineSegmentDistance(pointList[i], pointList[0], pointList[numberOfPoints - 1]);

            if (distance > maxDistance)
            {
                maxIndex = i;
                maxDistance = distance;
            }
        }

        if (maxDistance > parameters.DistanceThreshold.Value)
            return DivideForDouglasPeuckerSegment(pointList, parameters, maxIndex);

        return new List<T> { pointList[0], pointList[numberOfPoints - 1] };
    }

    private static List<T> DivideForDouglasPeuckerSegment<T>(List<T> pointList, SimplificationParameters parameters, int divideIndex) where T : IPoint, new()
    {
        var result = SimplifyByRamerDouglasPeuckerSegment(pointList.Take(divideIndex + 1).ToList(), parameters);

        result.AddRange(SimplifyByRamerDouglasPeuckerSegment(pointList.Skip(divideIndex).ToList(), parameters).Skip(1));

        return result;
    }

    /// <summary>
    /// BOPW measuring the distance to the chord <em>segment</em> instead of the infinite line through it (see
    /// <see cref="SimplifyByRamerDouglasPeuckerSegment{T}"/>); otherwise identical to <see cref="SimplifyByBeforeOpeningWindow{T}"/>.
    /// Added 2026-10-05 for the thin-part ablation (E12); not used by any published run.
    /// </summary>
    public static List<T> SimplifyByBeforeOpeningWindowSegment<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        double effectiveThreshold = parameters.DistanceThreshold.Value;

        int startIndex = 0, middleIndex = 1, endIndex = 2;

        while (endIndex < pointList.Count)
        {
            while (middleIndex < endIndex)
            {
                var distance = Helpers.TopologyUtility.GetPointToLineSegmentDistance(pointList[middleIndex], pointList[startIndex], pointList[endIndex]);

                if (distance > effectiveThreshold)
                {
                    result.Add(pointList[endIndex - 1]);

                    startIndex = endIndex - 1;

                    endIndex = middleIndex;

                    break;
                }

                middleIndex++;
            }

            middleIndex = startIndex + 1;
            endIndex++;
        }

        if (parameters.Retain3Points && result.Count == 1)
            result.Add(pointList[pointList.Count() / 2]);

        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
            result.Add(pointList.Last());

        return result;
    }

    /// <summary>
    /// Ramer-Douglas-Peucker for a ring stored without its closing vertex (<see cref="SimplificationParameters.IsRing"/>):
    /// the ring is closed by repeating its first vertex, simplified as a closed line (divided at the vertex farthest
    /// from the first one, Douglas and Peucker 1973, p. 117) and returned again without the closing vertex. A ring that
    /// already repeats its first vertex is simplified as it is.
    /// </summary>
    private static List<T> SimplifyRingAsClosedLine<T>(List<T> ring, SimplificationParameters parameters, Func<List<T>, SimplificationParameters, List<T>> simplify) where T : IPoint, new()
    {
        // the parts of a ring are open lines
        var lineParameters = parameters.Clone();

        lineParameters.IsRing = false;

        if (ring.First().Equals(ring.Last()))
            return simplify(ring, lineParameters);

        var result = simplify(ring.Append(ring[0]).ToList(), lineParameters);

        result.RemoveAt(result.Count - 1);

        // 2026-10-06: Retain3Points, as the other algorithms honour it. A ring divided at its farthest vertex keeps only
        // two vertices when both halves fit within the tolerance (a long thin polygon), which is no polygon any more;
        // keep the vertex farthest from the line through the two, i.e. the next one RDP itself would keep.
        // a tolerance larger than the whole ring leaves only the first vertex: keep the one farthest from it as well
        if (parameters.Retain3Points && result.Count == 1 && ring.Count >= 3)
        {
            var farthestFromFirst = Enumerable.Range(1, ring.Count - 1).OrderByDescending(i => SpatialUtility.GetEuclideanLength(ring[0], ring[i])).First();

            result.Add(ring[farthestFromFirst]);
        }

        if (parameters.Retain3Points && result.Count == 2 && ring.Count >= 3)
        {
            var second = ring.IndexOf(result[1]);

            int farthest = -1;
            double maxDistance = -1;

            for (int i = 1; i < ring.Count; i++)
            {
                if (i == second)
                    continue;

                var distance = SpatialUtility.GetPointToLineDistance(result[0], result[1], ring[i]);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthest = i;
                }
            }

            result.Insert(farthest < second ? 1 : 2, ring[farthest]);
        }

        return result;
    }

    private static List<T> DivideForDouglasPeucker<T>(List<T> pointList, SimplificationParameters parameters, int divideIndex) where T : IPoint, new()
    {
        var leftList = pointList.Take(divideIndex + 1).ToList();

        List<T> result = SimplifyByRamerDouglasPeucker(leftList, parameters);

        var rightList = pointList.Skip(divideIndex).ToList();

        var rightResult = SimplifyByRamerDouglasPeucker(rightList, parameters);

        result.AddRange(rightResult.Skip(1));

        return result;
    }



    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Li, Z. and Openshaw, S., 1992, Algorithms for automated line generalization based on a
    //      natural principle of objective generalization. IJGIS, 6(5), 373-389.
    // The "natural principle": everything inside the smallest visible object (SVO) of the target scale
    // collapses to one point. Two of the paper's three modes are implemented; neither returns a
    // subsequence of the input, both generate new points, so they are not candidates for the LRS
    // training labels. Not used by any research experiment.
    //
    // Vector mode: the SVO is a circle of diameter F (DistanceThreshold) centred at the point where the
    // line enters it; the line is followed to where it leaves the circle, and the midpoint of entry and
    // exit replaces everything in between; the exit point is the entry of the next SVO. First and last
    // points are kept.
    public static List<T> SimplifyByLiOpenshaw<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        var radius = parameters.DistanceThreshold.Value / 2;

        if (!(radius > 0))
            throw new ArgumentOutOfRangeException(nameof(parameters), "DistanceThreshold (the SVO diameter) must be positive");

        result.Add(pointList[0]);

        // the entry point of the current SVO (a vertex or an interpolated point on segment [segment, segment + 1])
        double cx = pointList[0].X, cy = pointList[0].Y;

        int segment = 0;

        while (true)
        {
            // first vertex outside the SVO
            int exit = -1;

            for (int k = segment + 1; k < pointList.Count; k++)
            {
                var dx = pointList[k].X - cx;
                var dy = pointList[k].Y - cy;

                if (dx * dx + dy * dy > radius * radius)
                {
                    exit = k;
                    break;
                }
            }

            if (exit < 0)
                break;

            // the segment that leaves the circle runs from the inside point (the previous vertex, or the entry
            // point when it lies on this segment) to pointList[exit]
            double ix = exit - 1 == segment ? cx : pointList[exit - 1].X;
            double iy = exit - 1 == segment ? cy : pointList[exit - 1].Y;

            var ex = pointList[exit].X;
            var ey = pointList[exit].Y;

            // |I + t (E - I) - C| = r, the root with t in [0, 1]
            double vx = ex - ix, vy = ey - iy, wx = ix - cx, wy = iy - cy;
            double a = vx * vx + vy * vy, b = 2 * (vx * wx + vy * wy), c = wx * wx + wy * wy - radius * radius;
            double t = (-b + Math.Sqrt(Math.Max(b * b - 4 * a * c, 0))) / (2 * a);

            double bx = ix + t * vx, by = iy + t * vy;

            result.Add(new T() { X = (cx + bx) / 2, Y = (cy + by) / 2 });

            cx = bx;
            cy = by;
            segment = exit - 1;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count / 2]);
        }

        if (SpatialUtility.GetEuclideanLength(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }

    // Raster mode: the plane is divided into square cells of side F (DistanceThreshold) anchored at the
    // origin; a run of consecutive vertices falling into the same cell is replaced by its centroid. The
    // first and last vertices are kept as they are (the rest of their runs is dropped).
    public static List<T> SimplifyByLiOpenshawRaster<T>(List<T> pointList, SimplificationParameters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        var cell = parameters.DistanceThreshold.Value;

        if (!(cell > 0))
            throw new ArgumentOutOfRangeException(nameof(parameters), "DistanceThreshold (the cell size) must be positive");

        (long, long) Cell(T p) => ((long)Math.Floor(p.X / cell), (long)Math.Floor(p.Y / cell));

        var last = pointList.Count - 1;

        result.Add(pointList[0]);

        int start = 0;

        var firstCell = Cell(pointList[0]);

        // skip the rest of the first point's run
        while (start + 1 < last && Cell(pointList[start + 1]) == firstCell)
            start++;

        int i = start + 1;

        while (i < last)
        {
            var key = Cell(pointList[i]);

            int j = i;

            double sx = 0, sy = 0;

            while (j < last && Cell(pointList[j]) == key)
            {
                sx += pointList[j].X;
                sy += pointList[j].Y;
                j++;
            }

            // a run that reaches the last point is represented by the last point itself
            if (j == last && Cell(pointList[last]) == key)
                break;

            result.Add(new T() { X = sx / (j - i), Y = sy / (j - i) });

            i = j;
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count / 2]);
        }

        result.Add(pointList[last]);

        return result;
    }

    #endregion

}
