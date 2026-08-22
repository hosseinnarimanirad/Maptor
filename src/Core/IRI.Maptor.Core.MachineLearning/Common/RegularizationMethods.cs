using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.MachineLearning;

public enum RegularizationMethods
{
    None = 1,
     
    /// <summary>
    /// Lasso: Least Absolute Shrinkage and Selection Operator
    /// </summary>
    L1 = 2,
     
    /// <summary>
    /// Ridge: Tikhonov regularization
    /// </summary>
    L2 = 3
}
