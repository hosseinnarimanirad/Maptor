using System;
using System.Linq;
using System.Collections.Generic;
using sb = IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;

namespace IRI.Maptor.Core.MachineLearning;

public static class SyntheticDataFactory
{
    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc0_5 =
            param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
            {
                (0.5, >= -2.5, <= 2.5) => false,
                (1.0, >= -2.0, <= 2.0) => false,
                (1.5, >= -2.0, <= 2.0) => false,
                (2.0, >= -1.5, <= 1.5) => false,
                //(2.5, >= -0.0, <= 0.0) => false,  
                _ => true
            };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc1_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -2.5, <= 2.5) => false,
               (0.5, >= -2.5, <= 2.5) => false,
               (1.0, >= -2.0, <= 2.0) => false,
               (1.5, >= -2.0, <= 2.0) => false,
               (2.0, >= -0.5, <= 0.5) => false,
               //(2.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc2_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -2.5, <= 2.5) => false,
               (0.5, >= -2.5, <= 2.5) => false,
               (1.0, >= -2.0, <= 2.0) => false,
               (1.5, >= -2.0, <= 2.0) => false,
               //(2.0, >= -1.0, <= 1.0) => false,
               //(2.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc4_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -2.5, <= 2.5) => false,
               (0.5, >= -2.5, <= 2.5) => false,
               (1.0, >= -2.0, <= 2.0) => false,
               (1.5, >= -1.5, <= 1.5) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc8_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               (0.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc16_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               (0.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc32_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               (0.5, >= -0.0, <= 0.0) => false,
               _ => true
           };

    public static readonly Predicate<(double dx1, double dx2, double dy)> IsRetainedFunc40_0 =
           param => (Math.Abs(param.dy), param.dx1, param.dx2) switch
           {
               (0.0, >= -0.0, <= 0.0) => false,
               _ => true
           };


    public static List<SyntheticDataItem> CreateAll()
    {
        return Create(0.5, IsRetainedFunc0_5)
                .Concat(Create(01.0, IsRetainedFunc1_0))
                .Concat(Create(02.0, IsRetainedFunc2_0))
                .Concat(Create(04.0, IsRetainedFunc4_0))
                .Concat(Create(08.0, IsRetainedFunc8_0))
                .Concat(Create(16.0, IsRetainedFunc16_0))
                .Concat(Create(32.0, IsRetainedFunc32_0))
                .ToList();
    }

    private static List<SyntheticDataItem> Create(double baseLength, Predicate<(double dx1, double dx2, double dy)> isRetainedFunc, int xOffset = 4, int yOffset = 4)
    {
        double pointY = 100;
        double startX = 100;
        double endX = startX + baseLength;

        double minX = startX - xOffset;
        double minY = pointY - yOffset;

        double maxX = endX + xOffset;
        double maxY = pointY + yOffset;

        var startPoint = new sb.Point(startX, pointY);
        var endPoint = new sb.Point(endX, pointY);

        List<SyntheticDataItem> result = new List<SyntheticDataItem>();

        var step = 0.5;

        for (double y = minY; y <= maxY; y += step)
        {
            for (double x = minX; x <= maxX; x += step)
            {
                var middlePoint = new sb.Point(x, y);

                var dx1 = x - startX;
                var dx2 = x - endX;
                var dy = pointY - y;

                bool retained = isRetainedFunc((dx1, dx2, dy));

                Geometry<sb.Point> original = Geometry<sb.Point>.CreatePointOrLineString([startPoint, middlePoint, endPoint], 0);

                Geometry<sb.Point> simplified = retained ? original : Geometry<sb.Point>.CreatePointOrLineString([startPoint, endPoint], 0);

                var rowNumber = (int)((y - minY) / step);

                result.Add(new SyntheticDataItem()
                {
                    OriginalLineString = original.AsWkt(),
                    SimplifiedLineString = simplified.AsWkt(),
                    Title = $"Base{baseLength}-Row{rowNumber} - {original.AsWkt()}",
                    Note = retained ? "RETAINED" : "REMOVED"
                });
            }
        }

        return result;
    }

}
