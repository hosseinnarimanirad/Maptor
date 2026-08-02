using System;
using System.Collections.Generic;

using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Spatial.Primitives;
using IRI.Maptor.Sta.Common.Abstractions;

namespace IRI.Maptor.Extensions;

public static class GeometryExtensions
{
    public static Geometry<T> Simplify<T>(
        this Geometry<T> geometry,
        LogisticSimplification<T> model,
        Func<T, T> toScreenMap,
        bool retain3Points) where T : IPoint, new()
    {
        Func<List<T>, List<T>> filter = pList => pList.SimplifyByLogisticRegression(model, toScreenMap, retain3Points);

        var temp = geometry.FilterPoints(filter);

        if (temp.NumberOfPoints > geometry.NumberOfPoints)
        {

        }

        return temp;
    }

    public static List<T> SimplifyByLogisticRegression<T>(
        this List<T> points,
        LogisticSimplification<T> model,
        Func<T, T> toScreenMap,
        bool retain3Points = false) where T : IPoint, new()
    {
        if (points.IsNullOrEmpty())
            return points;

        return model.SimplifyByLogisticRegression(points, toScreenMap, retain3Points);
        //return model.SimplifyByLogisticRegression_Fast_O_n(points, toScreenMap, retain3Points);
    }

}
