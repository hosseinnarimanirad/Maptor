using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Mathematics;

namespace IRI.Maptor.Sta.MachineLearning;


// David Cox in 1958
// The logistic regression is a form of supervised learning
public class LogisticRegression
{
    // 0.1 - 0.3
    private readonly double _learningRate = 0.1;

    private readonly int _maxIteration = 5000;

    private double[] beta = null;

    // پارامترهای ضرایب
    public double[] Beta { get { return beta; } }


    LogisticRegressionOptions _options;

    // Mean and standard deviation of xs
    public List<BasicStatisticsInfo> xStatistics { get; set; }

    public LogisticRegression(LogisticRegressionOptions options)
    {
        _options = options;
    }

    public LogisticRegression(double[] beta, List<BasicStatisticsInfo> xStatistics)
    {
        this.beta = beta;

        this.xStatistics = xStatistics;
    }

    public void Fit(Matrix xValues, double[] yValues)
    {
        // x: n*p
        // y: n*1

        var numberOfRow = xValues.NumberOfRows;
        var numberOfParameters = xValues.NumberOfColumns + 1;

        int iteration = 0;

        // *******************************************************
        // تخمین اولیه از ضرایب چند جمله‌ای
        // *******************************************************
        //beta = Enumerable.Range(1, numberOfParameters).Select(i => (double)1.0).ToArray();
        beta = Enumerable.Repeat(1.0, numberOfParameters).ToArray();


        if (_options.NormalizeFeatures)
        {
            // 1399.12.20
            // *******************************************************
            // ذخیره‌سازی پارامترها برای استفاده 
            // هنگام تخمین
            // *******************************************************
            xStatistics = xValues.GetStatisticsByColumns();
        }
        else
        {
            xStatistics = null;
        }
         
        // 1399.12.20
        // *******************************************************
        // ذخیره‌سازی پارامترها برای استفاده 
        // هنگام تخمین
        // *******************************************************
        //xStatistics = xValues.GetStatisticsByColumns();

        if (_options.NormalizeFeatures)
        {
            // *******************************************************
            // پیش پردازش داده
            // نرمال کردن داده‌ها
            // *******************************************************
            xValues = Normalization.NormalizeColumnsUsingZScore(xValues, this._options.VarianceCalculationMode);
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

        while (iteration < _maxIteration)
        {
            double[] yPredicted = new double[numberOfRow];

            double[] grad = new double[numberOfParameters];

            for (int i = 0; i < numberOfRow; i++)
            {
                var xs = xValues.GetRow(i);

                yPredicted[i] = LogisticRegressionHelper.CalculateLogisticFunction(xs, beta);

                // sigma [(yPredicate-y)*x]
                var error = yPredicted[i] - yValues[i];

                for (int xi = 0; xi < xs.Length; xi++)
                {
                    grad[xi] += error * xs[xi];
                }
            }

            // 1400.03.13
            // 1400.03.15
            // regularization strength
            var lambda = 1.0;
            //var sigmaBeta = this._options.RegularizationMethod == RegularizationMethods.None ? 0 : beta.Sum();

            for (int i = 0; i < numberOfParameters; i++)
            {
                double regularizationComponent = 0;

                if (i != 0)
                {
                    switch (this._options.RegularizationMethod)
                    {
                        case RegularizationMethods.L1:
                            regularizationComponent = lambda / (beta.Length);
                            break;

                        case RegularizationMethods.L2:
                            regularizationComponent = lambda / beta.Length * beta[i];
                            break;

                        case RegularizationMethods.None:
                        default:
                            break;
                    }
                }

                grad[i] = 1.0 / beta.Length * grad[i] + regularizationComponent;

                beta[i] = beta[i] - _learningRate * grad[i];
            }

            iteration++;
        }

        System.Diagnostics.Debug.WriteLine(string.Join(",", beta));
    }

    public double? Predict(List<double> xValues)
    {
        if (beta == null || xValues == null)
            return null;

        // 1400.03.14
        xValues.Insert(0, 1);

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
            for (int i = 1; i < xValues.Count; i++)
            {
                xValues[i] = Normalization.NormalizeUsingZScore(xValues[i], xStatistics[i].Mean, xStatistics[i].StandardDeviation);
            }
        }

        return LogisticRegressionHelper.CalculateLogisticFunction(xValues, Beta);
    }

    private Matrix ComputeTheFisherInformationMatrix_Copilot(Matrix xValues, double[] yValues)
    {
        var numberOfRow = xValues.NumberOfRows;

        var numberOfParameters = xValues.NumberOfColumns;

        Matrix fisherInformationMatrix = new Matrix(numberOfParameters, numberOfParameters);

        for (int i = 0; i < numberOfRow; i++)
        {
            var xs = xValues.GetRow(i);

            var yPredicted = LogisticRegressionHelper.CalculateLogisticFunction(xs, beta);

            var error = yPredicted - yValues[i];

            for (int j = 0; j < numberOfParameters; j++)
            {
                for (int k = 0; k < numberOfParameters; k++)
                {
                    fisherInformationMatrix[j, k] += xs[j] * xs[k] * yPredicted * (1 - yPredicted);
                }
            }
        }

        return fisherInformationMatrix;
    }

    private Matrix ComputeTheFisherInformationMatrix(Matrix xValues)
    {
        var numberOfRow = xValues.NumberOfRows;

        var numberOfParameters = xValues.NumberOfColumns;

        Matrix weight = new Matrix(numberOfRow, numberOfRow);

        for (int i = 0; i < numberOfRow; i++)
        {
            var xs = xValues.GetRow(i);

            var yPredicted = LogisticRegressionHelper.CalculateLogisticFunction(xs, beta);

            weight[i, i] = yPredicted * (1 - yPredicted);
        }

        return xValues.Transpose() * weight * xValues;
    }
     
}
