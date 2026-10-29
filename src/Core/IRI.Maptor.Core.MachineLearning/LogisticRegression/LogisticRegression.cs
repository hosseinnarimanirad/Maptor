using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.MachineLearning;


// David Cox in 1958
// The logistic regression is a form of supervised learning
public class LogisticRegression : IBinaryClassifier
{
    // coordinate-descent sweeps for one Newton step
    private readonly int _maxSweep = 10_000;

    private double[] beta = null;

    // پارامترهای ضرایب
    public double[] Beta { get { return beta; } }


    LogisticRegressionOptions _options;

    // Mean and standard deviation of xs
    public List<BasicStatisticsInfo> xStatistics { get; set; }

    public LogisticRegression(LogisticRegressionOptions options)
    {
        _options = options ?? new LogisticRegressionOptions();
    }

    /// <summary>
    /// Creates an already fitted model.
    /// </summary>
    /// <param name="beta">Coefficients, intercept first (the layout of <see cref="Beta"/>).</param>
    /// <param name="xStatistics">
    /// Statistics of the raw features in the layout <see cref="Fit"/> stores them: index 0 is a placeholder
    /// for the intercept and <c>xStatistics[i]</c> belongs to <c>beta[i]</c>. <c>null</c> means the features
    /// are not normalized.
    /// </param>
    public LogisticRegression(double[] beta, List<BasicStatisticsInfo> xStatistics)
    {
        this.beta = beta;

        this.xStatistics = xStatistics;

        _options = new LogisticRegressionOptions();
    }

    public void Fit(Matrix xValues, double[] yValues)
    {
        // x: n*p
        // y: n*1

        if (!(_options.RegularizationStrength >= 0))
            throw new ArgumentOutOfRangeException(nameof(LogisticRegressionOptions.RegularizationStrength), _options.RegularizationStrength, "must not be negative");

        if (_options.MaxIterations < 0)
            throw new ArgumentOutOfRangeException(nameof(LogisticRegressionOptions.MaxIterations), _options.MaxIterations, "must not be negative");

        if (!(_options.Tolerance >= 0))
            throw new ArgumentOutOfRangeException(nameof(LogisticRegressionOptions.Tolerance), _options.Tolerance, "must not be negative");

        var numberOfRow = xValues.NumberOfRows;
        var numberOfParameters = xValues.NumberOfColumns + 1;

        int iteration = 0;

        // *******************************************************
        // تخمین اولیه از ضرایب چند جمله‌ای
        // *******************************************************
        // zero: every row starts at probability 0.5, the usual starting point for Newton's method
        beta = new double[numberOfParameters];


        if (_options.NormalizeFeatures)
        {
            // *******************************************************
            // پیش پردازش داده
            // نرمال کردن داده‌ها
            // *******************************************************
            // 1399.12.20
            // *******************************************************
            // ذخیره‌سازی پارامترها برای استفاده
            // هنگام تخمین
            // *******************************************************
            // the statistics are the ones the normalization used (same variance mode),
            // so Predict standardizes new values exactly like the training values
            xValues = Normalization.NormalizeColumnsUsingZScore(xValues, this._options.VarianceCalculationMode, out var statistics);

            xStatistics = statistics;
        }
        else
        {
            xStatistics = null;

            // work on a copy: inserting the bias column below must not change the caller's matrix
            xValues = xValues.Clone();
        }

        // 1399.12.27
        // *******************************************************
        // پیش پردازش داده
        // به ارايه ایکس‌ها بایستی مقدار ۱ اضافه کرد
        // *******************************************************
        var ones = Enumerable.Repeat(1.0, numberOfRow).ToArray();
        xValues.InsertColumn(0, ones);

        if (_options.NormalizeFeatures)
        {
            xStatistics.Insert(0, new BasicStatisticsInfo() { Mean = 1, StandardDeviation = 0 });
        }

        // *******************************************************
        // Minimizes
        //   mean negative log-likelihood + (lambda / m) * penalty(beta_1..beta_p)
        // (L1: sum |beta_k|, L2: sum beta_k^2 / 2, intercept not penalized; m = number of rows)
        // with proximal Newton steps: coordinate descent on a quadratic model of the log-likelihood
        // plus the exact penalty (soft-thresholding for L1), then a backtracking line search.
        // Scaling by 1/m keeps the step independent of the number of rows.
        // *******************************************************
        var rows = Enumerable.Range(0, numberOfRow).Select(i => xValues.GetRow(i)).ToArray();

        var method = this._options.RegularizationMethod;

        // 1400.03.13
        // 1400.03.15
        // regularization strength
        var penalty = _options.RegularizationStrength / numberOfRow;

        var objective = CalculateObjective(rows, yValues, beta, method, penalty);

        var optimality = double.NaN;

        while (iteration < _options.MaxIterations)
        {
            CalculateGradientAndHessian(rows, yValues, beta, method, penalty, out var gradient, out var hessian);

            optimality = CalculateOptimality(gradient, beta, method, penalty);

            if (optimality <= _options.Tolerance)
                break;

            var direction = CalculateNewtonDirection(hessian, gradient, beta, method == RegularizationMethods.L1 ? penalty : 0);

            // change of the objective predicted for the full step (negative)
            var predictedChange = 0.0;

            for (int k = 0; k < numberOfParameters; k++)
            {
                predictedChange += gradient[k] * direction[k];

                if (method == RegularizationMethods.L1 && k != 0)
                {
                    predictedChange += penalty * (Math.Abs(beta[k] + direction[k]) - Math.Abs(beta[k]));
                }
            }

            // halve the step until the objective decreases enough (Armijo condition)
            var step = 1.0;

            var accepted = false;

            for (int i = 0; i < 60; i++)
            {
                var candidate = new double[numberOfParameters];

                for (int k = 0; k < numberOfParameters; k++)
                {
                    candidate[k] = beta[k] + step * direction[k];
                }

                var candidateObjective = CalculateObjective(rows, yValues, candidate, method, penalty);

                if (candidateObjective <= objective + 1e-4 * step * predictedChange)
                {
                    beta = candidate;

                    objective = candidateObjective;

                    accepted = true;

                    break;
                }

                step /= 2;
            }

            // no step decreases the objective any more: what is left is rounding
            if (!accepted)
                break;

            iteration++;
        }

        System.Diagnostics.Debug.WriteLine($"{string.Join(",", beta)} (iterations: {iteration}, optimality: {optimality})");
    }

    // mean negative log-likelihood plus the penalty; log(1 + e^z) is evaluated without overflow
    private static double CalculateObjective(double[][] rows, double[] yValues, double[] beta, RegularizationMethods method, double penalty)
    {
        double result = 0;

        for (int i = 0; i < rows.Length; i++)
        {
            var z = LogisticRegressionHelper.CalculateLogit(rows[i], beta)!.Value;

            result += (z > 0 ? z + Math.Log(1 + Math.Exp(-z)) : Math.Log(1 + Math.Exp(z))) - yValues[i] * z;
        }

        result /= rows.Length;

        for (int k = 1; k < beta.Length; k++)
        {
            switch (method)
            {
                case RegularizationMethods.L1:
                    result += penalty * Math.Abs(beta[k]);
                    break;

                case RegularizationMethods.L2:
                    result += penalty / 2 * beta[k] * beta[k];
                    break;

                case RegularizationMethods.None:
                default:
                    break;
            }
        }

        return result;
    }

    // gradient and Hessian of the smooth part of the objective (the log-likelihood and, for L2, the penalty)
    private static void CalculateGradientAndHessian(double[][] rows, double[] yValues, double[] beta, RegularizationMethods method, double penalty, out double[] gradient, out double[,] hessian)
    {
        var n = beta.Length;

        gradient = new double[n];

        hessian = new double[n, n];

        for (int i = 0; i < rows.Length; i++)
        {
            var xs = rows[i];

            var yPredicted = LogisticRegressionHelper.CalculateLogisticFunction(xs, beta);

            // sigma [(yPredicate-y)*x]
            var error = yPredicted - yValues[i];

            var weight = yPredicted * (1 - yPredicted);

            for (int j = 0; j < n; j++)
            {
                gradient[j] += error * xs[j];

                for (int k = 0; k <= j; k++)
                {
                    hessian[j, k] += weight * xs[j] * xs[k];
                }
            }
        }

        for (int j = 0; j < n; j++)
        {
            gradient[j] /= rows.Length;

            for (int k = 0; k <= j; k++)
            {
                hessian[j, k] /= rows.Length;

                hessian[k, j] = hessian[j, k];
            }
        }

        if (method == RegularizationMethods.L2)
        {
            // the intercept is not regularized
            for (int j = 1; j < n; j++)
            {
                gradient[j] += penalty * beta[j];

                hessian[j, j] += penalty;
            }
        }
    }

    // largest (sub)gradient component that can still decrease the objective; zero exactly at the optimum
    private static double CalculateOptimality(double[] gradient, double[] beta, RegularizationMethods method, double penalty)
    {
        double result = 0;

        for (int k = 0; k < gradient.Length; k++)
        {
            double value;

            if (method != RegularizationMethods.L1 || k == 0)
                value = Math.Abs(gradient[k]);
            else if (beta[k] != 0)
                value = Math.Abs(gradient[k] + penalty * Math.Sign(beta[k]));
            else
                value = Math.Max(Math.Abs(gradient[k]) - penalty, 0);

            result = Math.Max(result, value);
        }

        return result;
    }

    // minimizes  gradient.d + d.hessian.d / 2 + l1Penalty * sum(|beta_k + d_k|, k >= 1)  over d by cyclic coordinate descent
    private double[] CalculateNewtonDirection(double[,] hessian, double[] gradient, double[] beta, double l1Penalty)
    {
        var n = beta.Length;

        var direction = new double[n];

        // hessian * direction, kept up to date
        var hessianDirection = new double[n];

        var scale = 1 + beta.Max(b => Math.Abs(b));

        for (int sweep = 0; sweep < _maxSweep; sweep++)
        {
            double maxChange = 0;

            for (int k = 0; k < n; k++)
            {
                var a = hessian[k, k];

                // a zero column (e.g. a constant feature after normalization) cannot change the objective
                if (a <= 0)
                    continue;

                var b = gradient[k] + hessianDirection[k] - a * direction[k];

                var value = l1Penalty > 0 && k != 0
                    ? SoftThreshold(a * beta[k] - b, l1Penalty) / a - beta[k]
                    : -b / a;

                var change = value - direction[k];

                if (change == 0)
                    continue;

                direction[k] = value;

                for (int j = 0; j < n; j++)
                {
                    hessianDirection[j] += hessian[j, k] * change;
                }

                maxChange = Math.Max(maxChange, Math.Abs(change));
            }

            if (maxChange <= 1e-15 * scale)
                break;
        }

        return direction;
    }

    private static double SoftThreshold(double value, double threshold)
    {
        if (value > threshold)
            return value - threshold;

        if (value < -threshold)
            return value + threshold;

        return 0;
    }

    public double? Predict(List<double> xValues)
    {
        if (beta == null || xValues == null)
            return null;

        // 1400.03.14
        // work on a copy: the caller's list must stay as it was
        var values = new List<double>(xValues.Count + 1) { 1 };

        values.AddRange(xValues);

        if (xStatistics != null)
        {
            // *******************************************************
            // پیش پردازش داده
            // نرمال کردن داده‌ها
            //
            // 1400.03.10
            // ستون اول چون مقادیر ۱ هستند
            // نیازی به نرمال کردن ندارند
            // *******************************************************
            for (int i = 1; i < values.Count; i++)
            {
                values[i] = Normalization.NormalizeUsingZScore(values[i], xStatistics[i].Mean, xStatistics[i].StandardDeviation);
            }
        }

        return LogisticRegressionHelper.CalculateLogisticFunction(values, Beta);
    }
}
