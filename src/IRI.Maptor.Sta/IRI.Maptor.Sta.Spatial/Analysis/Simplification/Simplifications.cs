using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.Common.Helpers;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Sta.Spatial.Analysis;

public static class Simplifications
{
    public static List<T> SimplifyByAdditiveAreaPlus<T>(List<T> pointList, SimplificationParamters paramters) where T : IPoint
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
            var area = SpatialUtility.GetSignedTriangleArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            var areaCheck2 = SpatialUtility.GetUnsignedTriangleArea(pointList[firstIndex], pointList[(int)((firstIndex + thirdIndex) / 2.0)], pointList[thirdIndex]);

            tempArea += area;

            if (Math.Abs(tempArea) > paramters.AreaThreshold || areaCheck2 > paramters.AreaThreshold || (area * totalAreaCoef > 0))
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
    public static List<T> SimplifyByCumulativeAngle<T>(List<T> pointList, SimplificationParamters paramters) where T : IPoint
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
            var angle = SpatialUtility.GetSquareCosineOfAngle(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            if (angle < 0 || angle < paramters.AngleThreshold)
            {
                filtered.Add(secondIndex);

                firstIndex = secondIndex;
            }

            secondIndex = thirdIndex;

            thirdIndex = thirdIndex + 1;
        }

        if (paramters.Retain3Points && filtered.Count == 1)
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
    public static List<T> SimplifyByCumulativeAngleArea<T>(List<T> pointList, SimplificationParamters paramters) where T : IPoint
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
            var angle = SpatialUtility.GetSquareCosineOfAngle(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            var area = SpatialUtility.GetUnsignedTriangleArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

            tempArea += area;


            //if (Math.Abs(angle) < threshold)
            //{
            if ((angle < 0 || angle < paramters.AngleThreshold.Value) || tempArea > paramters.AreaThreshold.Value)
            {
                filtered.Add(secondIndex);

                firstIndex = secondIndex;

                tempArea = 0;
            }


            secondIndex = thirdIndex;

            thirdIndex = thirdIndex + 1;
        }

        if (paramters.Retain3Points && filtered.Count == 1)
        {
            filtered.Add(pointList.Count() / 2);
        }

        filtered.Add(pointList.Count - 1);

        var result = filtered.Select(i => pointList[i]).ToList();

        return result;
    }

    public static List<T> SimplifyByCumulativeEuclideanDistance<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        List<int> filtered = new List<int> { 0 };

        int firstIndex = 0, secondIndex = 1;

        var temp = 0.0;

        while (secondIndex < pointList.Count - 1)
        {
            var semiDistance = SpatialUtility.GetSquareEuclideanDistance(pointList[firstIndex], pointList[secondIndex]);

            temp += semiDistance;

            if (temp > parameters.DistanceThreshold.Value)
            {
                temp = 0;

                filtered.Add(secondIndex);

                firstIndex = secondIndex;
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
    public static List<T> SimplifyByAngle<T>(List<T> pointList, SimplificationParamters paramters) where T : IPoint
    {
        if (pointList.IsNullOrEmpty())
            return new List<T>();

        else if (pointList.Count < 3)
            return pointList;

        var angles = new List<(int index, double angle)>();

        for (int i = 0; i < pointList.Count - 2; i++)
        {
            var angle = SpatialUtility.GetSquareCosineOfAngle(pointList[i], pointList[i + 1], pointList[i + 2]);

            angles.Add((index: i + 1, angle: angle));
        }

        //When i.Item2 ~ 1 it means points are on a line
        var filter1 = angles.Where(i => i.angle < paramters.AngleThreshold).Select(i => i.index).ToList();

        filter1.Insert(0, 0);

        filter1.Add(pointList.Count - 1);

        var result = filter1.Select(i => pointList[i]).ToList();

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Tobler, W. R. (1966). Numerical map generalization: Department of Geography, University of
    //      Michigan Ann Arbour, MI, USA
    public static List<T> SimplifyByEuclideanDistance<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
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
            if (SpatialUtility.GetEuclideanDistance(pointList[i], pointList[firstIndex]) > parameters.DistanceThreshold)
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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Tobler, W. R. (1966). Numerical map generalization: Department of Geography, University of
    //      Michigan Ann Arbour, MI, USA
    public static List<T> SimplifyByNthPoint<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByRandomPointSelection<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        var numberOfPoints = pointList.Count;

        result.Add(pointList[0]);

        for (int i = 1; i < numberOfPoints; i++)
        {
            var random = RandomHelper.GetBetweenZeroAndOne();

            if (random > 0.5)
            {
                result.Add(pointList[i]);
            }
        }

        if (parameters.Retain3Points && result.Count == 1)
        {
            result.Add(pointList[pointList.Count() / 2]);
        }

        // prevent adding the last point if it is already added
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // ref: Lang, T., 1969, Rules for robot draughtsmen. Geographical Magazine, vol.62, No.1, pp.50-51
    public static List<T> SimplifyByLang<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;


        if (parameters.LookAhead == null)
        {
            parameters.LookAhead = Math.Max(3, pointList.Count / 10);
        }

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

        int endIndex = Math.Min(numberOfPoints - 1, parameters.LookAhead.Value);

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
                endIndex = Math.Min(numberOfPoints - 1, endIndex + parameters.LookAhead.Value);

                continue;
            }

            if (AnyPerpendicularDistanceExceedTolerance(pointList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList(), distanceThreshold))
            {
                endIndex--;
            }
            else
            {
                result.Add(pointList[endIndex]);
                startIndex = endIndex;
                endIndex = Math.Min(numberOfPoints - 1, endIndex + parameters.LookAhead.Value);
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
    public static List<T> SimplifyByRamerDouglasPeucker<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
    {
        var result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;


        //to handle lines with the same start and end
        if (pointList.First().Equals(pointList.Last()))
        {
            //return DivideForDouglasPeucker(pointList, threshold, pointList.Count / 2);
            return DivideForDouglasPeucker(pointList, parameters, pointList.Count / 2);
        }

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
            var perpendicularDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[0], pointList[numberOfPoints - 1], pointList[i]);

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
    public static List<T> SimplifyByReumannWitkam<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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
            var temporaryDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[startIndex], pointList[middleIndex], pointList[i]);

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByPerpendicularDistance<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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
            var perpendicularDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[i], pointList[i + 2], pointList[i + 1]);

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

        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.06.24
    // Modified methods means the traverse pattern has been modified
    public static List<T> SimplifyByModifiedPerpendicularDistance<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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

            var perpendicularDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[startIndex], pointList[midIndex], pointList[endIndex]);

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

        //    var perpendicularDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[startIndex], pointList[midIndex/*i + 2*/], pointList[i + 1]);

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

        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByVisvalingamWhyatt<T>(List<T> pointList, SimplificationParamters parameters, bool isRing) where T : IPoint
    {
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

        while (areas.Count > 0)
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
                    //    // (i-1, i, i+2): i+2 is used insted of i+1 because i+1 is going to be removed
                    //    var firstIndex = i - 1 < 0 ? pointCount - 1 : i - 1;
                    //    areas[firstIndex] = SpatialUtility.GetUnsignedTriangleArea(pList[firstIndex], pList[i], pList[(i + 2) % pointCount]);
                    //}
                    //if (isRing || i < areas.Count - 1)
                    //{
                    //    // (i, i+2, i+3): i is used insted of i+1 because i+1 is going to be removed
                    //    areas[(i + 1) % areaCount] = SpatialUtility.GetUnsignedTriangleArea(pList[i], pList[(i + 2) % pointCount], pList[(i + 3) % pointCount]);
                    //}

                    if (i > 0)
                    {
                        // (i-1, i, i+2): i+2 is used insted of i+1 because i+1 is going to be removed
                        var firstIndex = i - 1 < 0 ? pointCount - 1 : i - 1;

                        //if (i > areas.Count - 1) { }
                        //if (i + 2 > pList.Count - 1) { }
                        //if (firstIndex > areas.Count - 1) { }

                        areas[firstIndex] = SpatialUtility.GetUnsignedTriangleArea(pList[firstIndex], pList[i], pList[(i + 2) % pointCount]);
                    }

                    if (i < areas.Count - 1)
                    {
                        //if ((i + 1) % areaCount > areas.Count - 1) { }
                        //if ((i + 3) % pointCount > pList.Count - 1) { }

                        // (i, i+2, i+3): i is used insted of i+1 because i+1 is going to be removed
                        areas[(i + 1) % areaCount] = SpatialUtility.GetUnsignedTriangleArea(pList[i], pList[(i + 2) % pointCount], pList[(i + 3) % pointCount]);
                    }

                    //pList.RemoveAt((i + 1) % pointCount);
                    pList.RemoveAt(i + 1);
                    areas.RemoveAt(i);

                    break;
                }

            }

        }


        if (parameters.Retain3Points && pList.Count == 1)
        {
            pList.Add(pointList[pointList.Count() / 2]);
        }

        if (SpatialUtility.GetEuclideanDistance(pList.Last(), pointList.Last()) != 0)
        {
            pList.Add(pointList.Last());
        }

        return pList.ToList();
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.01.03
    // ref: Zhao, Z. and A. Saalfeld. Linear-time sleeve-fitting polyline simplification algorithms.
    //      In. Proceedings of AutoCarto 13. pages 214–223, 1997
    public static List<T> SimplifyBySleeveFitting<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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
                var temporaryDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[startIndex], pointList[endIndex], pointList[middleIndex]);

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByNormalOpeningWindow<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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
                var semiDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[startIndex], pointList[endIndex], pointList[middleIndex]);

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

        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByBeforeOpeningWindow<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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
                var semiDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[startIndex], pointList[endIndex], pointList[middleIndex]);

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

        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByTriangleRoutine<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
    {
        List<T> result = new List<T>();

        if (pointList.IsNullOrEmpty())
            return result;

        else if (pointList.Count < 3)
            return pointList;

        result.Add(pointList.First());

        for (int i = 0; i < pointList.Count - 2; i++)
        {
            var area = SpatialUtility.GetUnsignedTriangleArea(pointList[i], pointList[i + 1], pointList[i + 2]);

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // 1401.06.24
    public static List<T> SimplifyByModifiedTriangleRoutine<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
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

            var area = SpatialUtility.GetUnsignedTriangleArea(pointList[startIndex], pointList[midIndex/*i + 1*/], pointList[endIndex]);

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }


    // ***********************************************************************************************
    // ***********************************************************************************************
    // Threshold values are summed in each iteration
    public static List<T> SimplifyByCumulativeTriangleRoutine<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
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

            var area = SpatialUtility.GetUnsignedTriangleArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
        {
            result.Add(pointList.Last());
        }

        return result;
    }

    public static List<T> SimplifyByCumulativeTriangleRoutine2<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint
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

            var area = SpatialUtility.GetUnsignedTriangleArea(pointList[firstIndex], pointList[secondIndex], pointList[thirdIndex]);

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
        if (SpatialUtility.GetEuclideanDistance(result.Last(), pointList.Last()) != 0)
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
    public static List<T> SimplifyByAPSC<T>(List<T> pointList, SimplificationParamters parameters) where T : IPoint, new()
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

    private static List<T> DivideForDouglasPeucker<T>(List<T> pointList, SimplificationParamters paramters, int divideIndex) where T : IPoint, new()
    {
        var leftList = pointList.Take(divideIndex + 1).ToList();

        List<T> result = SimplifyByRamerDouglasPeucker(leftList, paramters);

        var rightList = pointList.Skip(divideIndex).ToList();

        var rightResult = SimplifyByRamerDouglasPeucker(rightList, paramters);

        result.AddRange(rightResult.Skip(1));

        return result;
    }



    private static bool AnyPerpendicularDistanceExceedTolerance<T>(List<T> pointList, double threshold) where T : IPoint, new()
    {
        for (int i = 1; i < pointList.Count - 1; i++)
        {
            var perpendicularDistance = SpatialUtility.GetPointToLineSegmentDistance(pointList[0], pointList[pointList.Count - 1], pointList[i]);

            if (perpendicularDistance >= threshold)
                return true;
        }

        return false;
    }

    #endregion

}
