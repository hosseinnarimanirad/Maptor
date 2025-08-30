using System;

using IRI.Maptor.Sta.Ogc;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Primitives;

namespace IRI.Maptor.Extensions;

public static class OgcFilterExtensions
{

    public static Func<Feature<Point>, bool> ParseFilter(this OgcFilter filter)
    {
        switch (filter.Predicate)
        {
            #region Comparison Operators

            case OgcPropertyIsLessThan ogcPropertyIsLessThan:
                return ParseFilter(ogcPropertyIsLessThan);

            case OgcPropertyIsGreaterThan ogcPropertyIsGreaterThan:
                return ParseFilter(ogcPropertyIsGreaterThan);

            case OgcPropertyIsLessThanOrEqualTo ogcPropertyIsLessThanOrEqualTo:
                return ParseFilter(ogcPropertyIsLessThanOrEqualTo);

            case OgcPropertyIsGreaterThanOrEqualTo ogcPropertyIsGreaterThanOrEqualTo:
                return ParseFilter(ogcPropertyIsGreaterThanOrEqualTo);

            case OgcPropertyIsEqualTo ogcPropertyIsEqualTo:
                return ParseFilter(ogcPropertyIsEqualTo);

            case OgcPropertyIsNotEqualTo ogcPropertyIsNotEqualTo:
                return ParseFilter(ogcPropertyIsNotEqualTo);

            case OgcPropertyIsLike ogcPropertyIsLike:
                return ParseFilter(ogcPropertyIsLike);

            case OgcPropertyIsNull ogcPropertyIsNull:
                return ParseFilter(ogcPropertyIsNull);

            case OgcPropertyIsNil ogcPropertyIsNil:
                return ParseFilter(ogcPropertyIsNil);

            case OgcPropertyIsBetween ogcPropertyIsBetween:
                return ParseFilter(ogcPropertyIsBetween);

            #endregion

            default:
                return f => false;
        }

    }

    #region Comparison Operators

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsLessThan predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (literal is null || propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return double.TryParse(f.Attributes[propertyName].ToString(), out var value) ? value < literal : false;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsGreaterThan predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (literal is null || propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return double.TryParse(f.Attributes[propertyName].ToString(), out var value) ? value > literal : false;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsLessThanOrEqualTo predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (literal is null || propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return double.TryParse(f.Attributes[propertyName].ToString(), out var value) ? value <= literal : false;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsGreaterThanOrEqualTo predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (literal is null || propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return double.TryParse(f.Attributes[propertyName].ToString(), out var value) ? value >= literal : false;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsEqualTo predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (literal is null || propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return f.Attributes[propertyName].ToString() == literal;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsNotEqualTo predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (literal is null || propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return f.Attributes[propertyName].ToString() != literal;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsLike predicate)
    {
        throw new NotImplementedException();
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsNull predicate)
    {
        var propertyName = predicate.GetPropertyName();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return f.Attributes[propertyName] is null;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsNil predicate)
    {
        var propertyName = predicate.GetPropertyName();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return f.Attributes[propertyName] is null;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsBetween predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var lowerBoundary = (predicate.LowerBoundary.Expression as OgcLiteral)?.GetDoubleValue();
        var upperBoundary = (predicate.UpperBoundary.Expression as OgcLiteral)?.GetDoubleValue();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (lowerBoundary is null ||
                    upperBoundary is null ||
                    propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                return double.TryParse(f.Attributes[propertyName].ToString(), out var value) ? value >= lowerBoundary && value <= upperBoundary : false;
            });
    }

    #endregion


}