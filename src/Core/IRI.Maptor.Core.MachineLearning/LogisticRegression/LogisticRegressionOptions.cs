using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.MachineLearning;

public class LogisticRegressionOptions
{
    public RegularizationMethods RegularizationMethod { get; set; }

    public VarianceCalculationMode VarianceCalculationMode { get; set; } = VarianceCalculationMode.Population;

    public bool NormalizeFeatures { get; set; } = true;

    /// <summary>
    /// Lambda: weight of the L1/L2 penalty against the total log-likelihood of the training rows.
    /// Equivalent to scikit-learn's <c>LogisticRegression(C = 1 / lambda)</c>. Must not be negative.
    /// </summary>
    public double RegularizationStrength { get; set; } = 1.0;

    /// <summary>
    /// Maximum number of Newton steps. A fit usually converges in fewer than 15. Must not be negative.
    /// </summary>
    public int MaxIterations { get; set; } = 100;

    /// <summary>
    /// The fit has converged when no component of the mean gradient (subgradient for L1) is larger than this.
    /// Must not be negative.
    /// </summary>
    public double Tolerance { get; set; } = 1e-10;

    public LogisticRegressionOptions()
    {
        this.RegularizationMethod = RegularizationMethods.None;
    }
}
