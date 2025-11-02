using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

            #region Logical Operators

            case OgcAnd ogcAnd:
                return ParseFilter(ogcAnd);

            case OgcOr ogcOr:
                return ParseFilter(ogcOr);

            case OgcNot ogcNot:
                return ParseFilter(ogcNot);

            #endregion

            #region Spatial Operators

            case OgcBBOX ogcBBOX:
                return ParseFilter(ogcBBOX);

            case OgcIntersects ogcIntersects:
                return ParseFilter(ogcIntersects);

            case OgcContains ogcContains:
                return ParseFilter(ogcContains);

            case OgcWithin ogcWithin:
                return ParseFilter(ogcWithin);

            case OgcDisjoint ogcDisjoint:
                return ParseFilter(ogcDisjoint);

            case OgcTouches ogcTouches:
                return ParseFilter(ogcTouches);

            case OgcOverlaps ogcOverlaps:
                return ParseFilter(ogcOverlaps);

            case OgcCrosses ogcCrosses:
                return ParseFilter(ogcCrosses);

            case OgcEqualsSpatialy ogcEqualsSpatialy:
                return ParseFilter(ogcEqualsSpatialy);

            case OgcDWithin ogcDWithin:
                return ParseFilter(ogcDWithin);

            case OgcBeyond ogcBeyond:
                return ParseFilter(ogcBeyond);

            #endregion

            default:
                return f => false;
        }

    }

    #region Helper Methods

    /// <summary>
    /// Creates a numeric comparison filter with culture-invariant parsing
    /// </summary>
    private static Func<Feature<Point>, bool> CreateNumericComparisonFilter(
        string propertyName,
        double? literal,
        Func<double, double, bool> comparison)
    {
        return new Func<Feature<Point>, bool>(f =>
        {
            if (propertyName is null || !f.Attributes.ContainsKey(propertyName))
                return false;

            var attribute = f.Attributes[propertyName]?.ToString();

            if (literal is null) return attribute is null;

            return double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? comparison(value, literal.Value)
                : false;
        });
    }

    /// <summary>
    /// Converts OGC wildcard pattern to .NET Regex pattern
    /// </summary>
    private static string ConvertOgcPatternToRegex(string pattern, string wildCard, string singleChar, string escapeChar)
    {
        var result = new StringBuilder("^");
        var skipNext = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            if (skipNext)
            {
                skipNext = false;
                continue;
            }

            var currentChar = pattern[i].ToString();

            if (i > 0 && pattern[i - 1].ToString() == escapeChar)
            {
                // Previous char was escape, treat current as literal
                result.Append(Regex.Escape(currentChar));
            }
            else if (currentChar == escapeChar)
            {
                // Skip escape char itself
                continue;
            }
            else if (currentChar == wildCard)
            {
                // Wildcard matches zero or more characters
                result.Append(".*");
            }
            else if (currentChar == singleChar)
            {
                // Single char wildcard matches exactly one character
                result.Append(".");
            }
            else
            {
                // Regular character, escape for regex
                result.Append(Regex.Escape(currentChar));
            }
        }

        result.Append("$");
        return result.ToString();
    }

    #endregion

    #region Comparison Operators

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsLessThan predicate)
    {
        return CreateNumericComparisonFilter(
            predicate.GetPropertyName(),
            predicate.GetLiteral(),
            (value, literal) => value < literal);
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsGreaterThan predicate)
    {
        return CreateNumericComparisonFilter(
            predicate.GetPropertyName(),
            predicate.GetLiteral(),
            (value, literal) => value > literal);
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsLessThanOrEqualTo predicate)
    {
        return CreateNumericComparisonFilter(
            predicate.GetPropertyName(),
            predicate.GetLiteral(),
            (value, literal) => value <= literal);
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsGreaterThanOrEqualTo predicate)
    {
        return CreateNumericComparisonFilter(
            predicate.GetPropertyName(),
            predicate.GetLiteral(),
            (value, literal) => value >= literal);
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsEqualTo predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                var attribute = f.Attributes[propertyName]?.ToString();

                if (literal is null) return attribute is null;

                return attribute == literal;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsNotEqualTo predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var literal = predicate.GetLiteral();

        return
            new Func<Feature<Point>, bool>(f =>
            {
                if (propertyName is null || !f.Attributes.ContainsKey(propertyName)) return false;

                var attribute = f.Attributes[propertyName]?.ToString();

                if (literal is null) return attribute is not null;

                return attribute != literal;
            });
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcPropertyIsLike predicate)
    {
        var propertyName = predicate.GetPropertyName();
        var pattern = (predicate.Expressions.FirstOrDefault(e => e is OgcLiteral) as OgcLiteral)?.Value;

        var wildCard = predicate.WildCard ?? "*";
        var singleChar = predicate.SingleChar ?? "?";
        var escapeChar = predicate.EscapeChar ?? "\\";
        var matchCase = predicate.MatchCase;

        return new Func<Feature<Point>, bool>(f =>
        {
            if (propertyName is null || pattern is null || !f.Attributes.ContainsKey(propertyName))
                return false;

            var attribute = f.Attributes[propertyName]?.ToString();
            if (attribute is null) return false;

            // Convert OGC pattern to .NET regex
            var regexPattern = ConvertOgcPatternToRegex(pattern, wildCard, singleChar, escapeChar);
            var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

            return Regex.IsMatch(attribute, regexPattern, regexOptions);
        });
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

        return new Func<Feature<Point>, bool>(f =>
        {
            if (lowerBoundary is null ||
                upperBoundary is null ||
                propertyName is null ||
                !f.Attributes.ContainsKey(propertyName))
                return false;

            var attribute = f.Attributes[propertyName]?.ToString();

            if (attribute is null) return false;

            return double.TryParse(attribute, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value >= lowerBoundary && value <= upperBoundary
                : false;
        });
    }

    #endregion

    #region Logical Operators

    private static Func<Feature<Point>, bool> ParseFilter(this OgcAnd predicate)
    {
        var predicateFunctions = predicate.Predicates
            .Select(p => new OgcFilter { Predicate = p }.ParseFilter())
            .ToList();

        return new Func<Feature<Point>, bool>(f =>
            predicateFunctions.All(func => func(f)));
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcOr predicate)
    {
        var predicateFunctions = predicate.Predicates
            .Select(p => new OgcFilter { Predicate = p }.ParseFilter())
            .ToList();

        return new Func<Feature<Point>, bool>(f =>
            predicateFunctions.Any(func => func(f)));
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcNot predicate)
    {
        var predicateFunction = new OgcFilter { Predicate = predicate.Predicate as OgcFilterBase }.ParseFilter();

        return new Func<Feature<Point>, bool>(f => !predicateFunction(f));
    }

    #endregion

    #region Spatial Operators

    // Note: Spatial operators require geometry parsing from GML which is not yet implemented.
    // These are placeholder implementations that can be completed when geometry parsing is available.

    private static Func<Feature<Point>, bool> ParseFilter(this OgcBBOX predicate)
    {
        // TODO: Implement BBOX spatial filter
        // Requires parsing GML envelope from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcIntersects predicate)
    {
        // TODO: Implement Intersects spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcContains predicate)
    {
        // TODO: Implement Contains spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcWithin predicate)
    {
        // TODO: Implement Within spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcDisjoint predicate)
    {
        // TODO: Implement Disjoint spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcTouches predicate)
    {
        // TODO: Implement Touches spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcOverlaps predicate)
    {
        // TODO: Implement Overlaps spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcCrosses predicate)
    {
        // TODO: Implement Crosses spatial filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcEqualsSpatialy predicate)
    {
        // TODO: Implement Equals (spatial) filter
        // Requires parsing GML geometry from predicate.Expressions
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcDWithin predicate)
    {
        // TODO: Implement DWithin spatial filter (within distance)
        // Requires parsing GML geometry from predicate.Expressions and using predicate.Distance
        return f => true; // Placeholder: returns true for all features
    }

    private static Func<Feature<Point>, bool> ParseFilter(this OgcBeyond predicate)
    {
        // TODO: Implement Beyond spatial filter (beyond distance)
        // Requires parsing GML geometry from predicate.Expressions and using predicate.Distance
        return f => true; // Placeholder: returns true for all features
    }

    #endregion

}