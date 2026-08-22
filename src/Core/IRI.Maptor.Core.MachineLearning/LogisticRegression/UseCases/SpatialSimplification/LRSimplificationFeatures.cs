namespace IRI.Maptor.Core.MachineLearning;

/// <summary>
/// Logistic Regression Simplification Features
/// </summary>
public enum LRSimplificationFeatures
{
    VerticalDistance = 1,
    DistanceToPrevious = 2,
    DistanceToNext = 3,
    Area = 4,
    SquareCosineOfAngle = 5,
    CosineOfAngle = 6,

    // فاصله نقطه اول تا سوم
    BaseLength = 7,

    // abs(x1 - x2)
    dX12 = 10,
    
    // abs(x1 - x3)
    dX13 = 11,
    
    // abs(x2 - x3)
    dX23 = 12,

    // abs(y1 - y2)
    dY12 = 13,

    // abs(y1 - y3)
    dY13 = 14,

    // abs(y2 - y3)
    dY23 = 15
}

