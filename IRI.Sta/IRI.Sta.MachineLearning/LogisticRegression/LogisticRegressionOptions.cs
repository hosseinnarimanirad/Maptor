﻿using IRI.Sta.Mathematics; 

namespace IRI.Sta.MachineLearning;

public class LogisticRegressionOptions
{
    public RegularizationMethods RegularizationMethod { get; set; }

    public VarianceCalculationMode VarianceCalculationMode { get; set; } = VarianceCalculationMode.Population;

    public bool NormalizeFeatures { get; set; } = true;

    public LogisticRegressionOptions()
    {
        this.RegularizationMethod = RegularizationMethods.None;
    }
}
