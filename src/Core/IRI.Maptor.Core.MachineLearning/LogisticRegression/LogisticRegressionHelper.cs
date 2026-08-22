using System;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.MachineLearning;

public static class LogisticRegressionHelper
{
    // logit function
    // f(x) = b_0 + b_1 * x_1 + b_2 * x_2 + … + b_n * x_n
    public static double? CalculateLogit(double[] x, double[] weights)
    {
        return x.DotProduct(weights);
    }

    public static double? CalculateLogit(List<double> x, double[] weights)
    {
        return x.DotProduct(weights);
    }

    // باید دقت داشت که اولین مقدار ایکس
    // برابر است با عدد یک به این ترتیب
    // طول بردار ظرایب و ایکس یکی می‌شود
    // صفحه ۹۵ کتاب زیر
    // Introduction to Algorithms for Data Mining and Machine Learning, Academic Press, 2019
    public static double CalculateLogisticFunction(double[] x, double[] weights)
    {
        var zValue = CalculateLogit(x, weights);

        if (zValue == null)
            throw new NotImplementedException("LogisticRegressionHelper > CalculateLogisticFunction");

        return Sigmoid.CalculateSigmoid(zValue.Value);
    }

    public static double CalculateLogisticFunction(List<double> x, double[] weights)
    {
        var zValue = CalculateLogit(x, weights);

        if (zValue == null)
            throw new NotImplementedException("LogisticRegressionHelper > CalculateLogisticFunction");

        return Sigmoid.CalculateSigmoid(zValue.Value);
    }

    public static double CalculateNegLogLikelihood(double y, double yPredicted)
    {
        return -1 * (y * Math.Log(yPredicted) + (1 - y) * Math.Log(1 - yPredicted));
    }

    // link: https://philippmuens.com/logistic-regression-from-scratch
    public static double CalculateNegLogLikelihood(double y, double[] x, double[] weights)
    {
        var yPredicted = CalculateLogisticFunction(x, weights);

        return CalculateNegLogLikelihood(y, yPredicted);
    }

    public static double CalculateNegLogLikelihood(double[] y, double[][] x, double[] weights)
    {
        double length = y.Length;

        double result = 0;

        for (int i = 0; i < length; i++)
        {
            result += CalculateNegLogLikelihood(y[i], x[i], weights);
        }

        return 1.0 / length * result;
    }

    public static double CalculateLossByGradientDescent(double[] y, double[] yPredicted)
    {
        if (y.Length != yPredicted.Length)
            throw new NotImplementedException("LogisticRegressionHelper > CalculateLossByGradientDescent");

        var length = y.Length;

        double sum = 0.0;

        for (int i = 0; i < y.Length; i++)
        {
            sum += CalculateNegLogLikelihood(y[i], yPredicted[i]);
        }

        return 1.0 / length * sum;
    }
}
