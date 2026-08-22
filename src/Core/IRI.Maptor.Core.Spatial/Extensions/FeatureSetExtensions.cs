using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Spatial.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class FeatureSetExtensions
{
    public static bool IsNullOrEmpty<T>(this FeatureSet<T> featureSet) where T : IPoint, new()
    {
        return featureSet is null || featureSet.HasNoGeometry();
    }

}
