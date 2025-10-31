using IRI.Maptor.Sta.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Tst.NetCore.MachineLearning
{
    public class LogisticRegressionTest
    {
        #region Sample Data #1

        private const string sampleNormalizeInput =
            @"34.62365962451697,78.0246928153624,0
            30.28671076822607,43.89499752400101,0
            35.84740876993872,72.90219802708364,0
            60.18259938620976,86.30855209546826,1
            79.0327360507101,75.3443764369103,1
            45.08327747668339,56.3163717815305,0
            61.10666453684766,96.51142588489624,1
            75.02474556738889,46.55401354116538,1
            76.09878670226257,87.42056971926803,1
            84.43281996120035,43.53339331072109,1
            95.86155507093572,38.22527805795094,0
            75.01365838958247,30.60326323428011,0
            82.30705337399482,76.48196330235604,1
            69.36458875970939,97.71869196188608,1
            39.53833914367223,76.03681085115882,0
            53.9710521485623,89.20735013750205,1
            69.07014406283025,52.74046973016765,1
            67.94685547711617,46.67857410673128,0
            70.66150955499435,92.92713789364831,1
            76.97878372747498,47.57596364975532,1
            67.37202754570876,42.83843832029179,0
            89.67677575072079,65.79936592745237,1
            50.534788289883,48.85581152764205,0
            34.21206097786789,44.20952859866288,0
            77.9240914545704,68.9723599933059,1
            62.27101367004632,69.95445795447587,1
            80.1901807509566,44.82162893218353,1
            93.114388797442,38.80067033713209,0
            61.83020602312595,50.25610789244621,0
            38.78580379679423,64.99568095539578,0
            61.379289447425,72.80788731317097,1
            85.40451939411645,57.05198397627122,1
            52.10797973193984,63.12762376881715,0
            52.04540476831827,69.43286012045222,1
            40.23689373545111,71.16774802184875,0
            54.63510555424817,52.21388588061123,0
            33.91550010906887,98.86943574220611,0
            64.17698887494485,80.90806058670817,1
            74.78925295941542,41.57341522824434,0
            34.1836400264419,75.2377203360134,0
            83.90239366249155,56.30804621605327,1
            51.54772026906181,46.85629026349976,0
            94.44336776917852,65.56892160559052,1
            82.36875375713919,40.61825515970618,0
            51.04775177128865,45.82270145776001,0
            62.22267576120188,52.06099194836679,0
            77.19303492601364,70.45820000180959,1
            97.77159928000232,86.7278223300282,1
            62.07306379667647,96.76882412413983,1
            91.56497449807442,88.69629254546599,1
            79.94481794066932,74.16311935043758,1
            99.2725269292572,60.99903099844988,1
            90.54671411399852,43.39060180650027,1
            34.52451385320009,60.39634245837173,0
            50.2864961189907,49.80453881323059,0
            49.58667721632031,59.80895099453265,0
            97.64563396007767,68.86157272420604,1
            32.57720016809309,95.59854761387875,0
            74.24869136721598,69.82457122657193,1
            71.79646205863379,78.45356224515052,1
            75.3956114656803,85.75993667331619,1
            35.28611281526193,47.02051394723416,0
            56.25381749711624,39.26147251058019,0
            30.05882244669796,49.59297386723685,0
            44.66826172480893,66.45008614558913,0
            66.56089447242954,41.09209807936973,0
            40.45755098375164,97.53518548909936,1
            49.07256321908844,51.88321182073966,0
            80.27957401466998,92.11606081344084,1
            66.74671856944039,60.99139402740988,1
            32.72283304060323,43.30717306430063,0
            64.0393204150601,78.03168802018232,1
            72.34649422579923,96.22759296761404,1
            60.45788573918959,73.09499809758037,1
            58.84095621726802,75.85844831279042,1
            99.82785779692128,72.36925193383885,1
            47.26426910848174,88.47586499559782,1
            50.45815980285988,75.80985952982456,1
            60.45555629271532,42.50840943572217,0
            82.22666157785568,42.71987853716458,0
            88.9138964166533,69.80378889835472,1
            94.83450672430196,45.69430680250754,1
            67.31925746917527,66.58935317747915,1
            57.23870631569862,59.51428198012956,1
            80.36675600171273,90.96014789746954,1
            68.46852178591112,85.59430710452014,1
            42.0754545384731,78.84478600148043,0
            75.47770200533905,90.42453899753964,1
            78.63542434898018,96.64742716885644,1
            52.34800398794107,60.76950525602592,0
            94.09433112516793,77.15910509073893,1
            90.44855097096364,87.50879176484702,1
            55.48216114069585,35.57070347228866,0
            74.49269241843041,84.84513684930135,1
            89.84580670720979,45.35828361091658,1
            83.48916274498238,48.38028579728175,1
            42.2617008099817,87.10385094025457,1
            99.31500880510394,68.77540947206617,1
            55.34001756003703,64.9319380069486,1
            74.77589300092767,89.52981289513276,1";

        private List<(double x0, double x1, double y)> GetDataSet()
        {
            var lines = sampleNormalizeInput.Split('\n');



            return lines.Select(l => l.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .Select(s =>
                        (
                             //1.0,
                             double.Parse(s[0]),
                             double.Parse(s[1]),
                             double.Parse(s[2])
                        ))
                        .ToList();
        }

        #endregion

        #region Sample Data #2

        static readonly double[] _gmat = [780, 750, 690, 710, 680, 730, 690, 720, 740, 690, 610, 690, 710, 680, 770, 610, 580, 650, 540, 590, 620, 600, 550, 550, 570, 670, 660, 580, 650, 660, 640, 620, 660, 660, 680, 650, 670, 580, 590, 690];
        static readonly double[] _gpa = [4, 3.9, 3.3, 3.7, 3.9, 3.7, 2.3, 3.3, 3.3, 1.7, 2.7, 3.7, 3.7, 3.3, 3.3, 3, 2.7, 3.7, 2.7, 2.3, 3.3, 2, 2.3, 2.7, 3, 3.3, 3.7, 2.3, 3.7, 3.3, 3, 2.7, 4, 3.3, 3.3, 2.3, 2.7, 3.3, 1.7, 3.7];
        static readonly double[] _work_experience = [3, 4, 3, 5, 4, 6, 1, 4, 5, 1, 3, 5, 6, 4, 3, 1, 4, 6, 2, 3, 2, 1, 4, 1, 2, 6, 4, 2, 6, 5, 1, 2, 4, 6, 5, 1, 2, 1, 4, 5];

        IRI.Maptor.Sta.Mathematics.Matrix _gmat_gpa_workExperience = new IRI.Maptor.Sta.Mathematics.Matrix([_gmat, _gpa, _work_experience]);

        static readonly double[] _admitted = [1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1];

        #endregion

        [Fact]
        public void TestBaseFunctions()
        {

            Assert.Equal(0.5, Sigmoid.CalculateSigmoid(0));

            Assert.Equal(70, LogisticRegressionHelper.CalculateLogit(new double[] { 1, 2, 3, 4 }, new double[] { 5, 6, 7, 8 }));

            Assert.Equal(1, LogisticRegressionHelper.CalculateLogisticFunction(new double[] { 1, 2, 3, 4 }, new double[] { 5, 6, 7, 8 }));

            Assert.True(LogisticRegressionHelper.CalculateNegLogLikelihood(1, 0.1) > 2.30 && LogisticRegressionHelper.CalculateNegLogLikelihood(1, 0.1) < 2.31);
            Assert.True(LogisticRegressionHelper.CalculateNegLogLikelihood(0, 0.9) > 2.30 && LogisticRegressionHelper.CalculateNegLogLikelihood(0, 0.9) < 2.31);
            Assert.True(LogisticRegressionHelper.CalculateNegLogLikelihood(1, 0.9) > 0.10 && LogisticRegressionHelper.CalculateNegLogLikelihood(1, 0.9) < 0.11);
            Assert.True(LogisticRegressionHelper.CalculateNegLogLikelihood(0, 0.1) > 0.10 && LogisticRegressionHelper.CalculateNegLogLikelihood(0, 0.1) < 0.11);


            Assert.True(LogisticRegressionHelper.CalculateLossByGradientDescent([1], [0.1]) > 2.30 &&
                            LogisticRegressionHelper.CalculateLossByGradientDescent([1], [0.1]) < 2.31);
            Assert.True(LogisticRegressionHelper.CalculateLossByGradientDescent([0], [0.9]) > 2.30 &&
                            LogisticRegressionHelper.CalculateLossByGradientDescent([0], [0.9]) < 2.31);
            Assert.True(LogisticRegressionHelper.CalculateLossByGradientDescent([1], [0.9]) > 0.10 &&
                            LogisticRegressionHelper.CalculateLossByGradientDescent([1], [0.9]) < 0.11);
            Assert.True(LogisticRegressionHelper.CalculateLossByGradientDescent([0], [0.1]) > 0.10 &&
                            LogisticRegressionHelper.CalculateLossByGradientDescent([0], [0.1]) < 0.11);

            //assert 2.30 < error([1], [0.1]) < 2.31
            //assert 2.30 < error([0], [0.9]) < 2.31
            //assert 0.10 < error([1], [0.9]) < 0.11
            //assert 0.10 < error([0], [0.1]) < 0.11
        }

        [Fact]
        public void TestNormalize()
        {
            var dataSet = GetDataSet();

            IRI.Maptor.Sta.Mathematics.Matrix matrix = new IRI.Maptor.Sta.Mathematics.Matrix(dataSet.Count, 2);

            var ones = Enumerable.Repeat(1.0, matrix.NumberOfRows).ToArray();

            matrix.SetColumn(0, dataSet.Select(i => i.x0).ToArray());
            matrix.SetColumn(1, dataSet.Select(i => i.x1).ToArray());

            var normalizedMatrix = Normalization.NormalizeColumnsUsingZScore(matrix, IRI.Maptor.Sta.Mathematics.VarianceCalculationMode.Sample);

            normalizedMatrix.InsertColumn(0, ones);

            Assert.Equal(1.0, normalizedMatrix[0, 0]);
            Assert.Equal(1.0, normalizedMatrix[1, 0]);
            Assert.Equal(1.0, normalizedMatrix[2, 0]);
            Assert.Equal(1.0, normalizedMatrix[3, 0]);
            Assert.Equal(1.0, normalizedMatrix[4, 0]);

            Assert.Equal(-1.5942162646576388, normalizedMatrix[0, 1]);
            Assert.Equal(-1.8171014180340745, normalizedMatrix[1, 1]);
            Assert.Equal(-1.531325157335502, normalizedMatrix[2, 1]);
            Assert.Equal(-0.28068723821760927, normalizedMatrix[3, 1]);
            Assert.Equal(0.6880619310375534, normalizedMatrix[4, 1]);

            Assert.Equal(0.6351413941754435, normalizedMatrix[0, 2]);
            Assert.Equal(-1.2014885239142388, normalizedMatrix[1, 2]);
            Assert.Equal(0.3594832875590465, normalizedMatrix[2, 2]);
            Assert.Equal(1.0809228071415948, normalizedMatrix[3, 2]);
            Assert.Equal(0.4909048515228952, normalizedMatrix[4, 2]);

            //[[1, -1.5942162646576388, 0.6351413941754435],
            //[1, -1.8171014180340745, -1.2014885239142388],
            //[1, -1.531325157335502, 0.3594832875590465],
            //[1, -0.28068723821760927, 1.0809228071415948],
            //[1, 0.6880619310375534, 0.4909048515228952]]

        }

        [Fact]
        public void TestLogisticsRegressionCoefficients_UsingSampleVariance()
        {
            var dataSet = GetDataSet();

            IRI.Maptor.Sta.Mathematics.Matrix matrix = new IRI.Maptor.Sta.Mathematics.Matrix(dataSet.Count, 2);

            matrix.SetColumn(0, dataSet.Select(i => i.x0).ToArray());
            matrix.SetColumn(1, dataSet.Select(i => i.x1).ToArray());

            LogisticRegression lr = new LogisticRegression(new LogisticRegressionOptions()
            {
                RegularizationMethod = RegularizationMethods.None,
                VarianceCalculationMode = IRI.Maptor.Sta.Mathematics.VarianceCalculationMode.Sample
            });

            lr.Fit(matrix, dataSet.Select(d => d.y).ToArray());

            //1.7184494794195502,4.012902517516046,3.7439030395950157
            Assert.Equal(1.7184, lr.Beta[0], 4);
            Assert.Equal(4.013, lr.Beta[1], 3);
            Assert.Equal(3.744, lr.Beta[2], 3);
        }

        [Fact]
        public void TestLogisticsRegressionCoefficients_Regularization()
        {
            // ***************** No Regularization *************************
            // *************************************************************
            LogisticRegression lr = new LogisticRegression(new LogisticRegressionOptions()
            {
                RegularizationMethod = RegularizationMethods.None,
                VarianceCalculationMode = IRI.Maptor.Sta.Mathematics.VarianceCalculationMode.Population
            });

            lr.Fit(_gmat_gpa_workExperience, _admitted);

            //-0.71303637, 0.95384204, 1.52857878, 1.72106216
            Assert.Equal(-0.71303637, lr.Beta[0], 3);
            Assert.Equal(0.95384204, lr.Beta[1], 2);
            Assert.Equal(1.52857878, lr.Beta[2], 2);
            Assert.Equal(1.72106216, lr.Beta[3], 3);

            // ***************** L2 Regularization *************************
            // *************************************************************
            // LogisticRegression(penalty='l2', C = 1)
            lr = new LogisticRegression(new LogisticRegressionOptions()
            {
                RegularizationMethod = RegularizationMethods.L2,
                VarianceCalculationMode = IRI.Maptor.Sta.Mathematics.VarianceCalculationMode.Population
            });

            lr.Fit(_gmat_gpa_workExperience, _admitted);

            // -0.40313989, 0.82707083, 1.06912852, 1.21640253
            Assert.Equal(-0.40313989, lr.Beta[0], 4);
            Assert.Equal(0.82707083, lr.Beta[1], 4);
            Assert.Equal(1.06912852, lr.Beta[2], 4);
            Assert.Equal(1.21640253, lr.Beta[3], 4);

            // ***************** L1 Regularization *************************
            // *************************************************************
            // LogisticRegression(solver='saga', penalty='l1', C = 1)
            lr = new LogisticRegression(new LogisticRegressionOptions()
            {
                RegularizationMethod = RegularizationMethods.L1,
                VarianceCalculationMode = IRI.Maptor.Sta.Mathematics.VarianceCalculationMode.Population
            });

            lr.Fit(_gmat_gpa_workExperience, _admitted);

            // -0.41339044, 0.71783591, 1.12285765, 1.31522002
            // -0.41344991, 0.71763310, 1.12324844, 1.31504399
            Assert.Equal(-0.41339044, lr.Beta[0], 3);
            Assert.Equal(0.71783591, lr.Beta[1], 3);
            Assert.Equal(1.12285765, lr.Beta[2], 3);
            Assert.Equal(1.31522002, lr.Beta[3], 3);
        }

        [Fact]
        public void TestLogisticsRegressionCoefficients_WithRegularization()
        {
            // python equivalent: logistic_regression = LogisticRegression(penalty = 'l2', C = 1)

        }

        // ref: https://datatofish.com/logistic-regression-python/
        [Fact]
        public void TestLogisticRegressionPredict()
        {
            //var gmat = new double[] { 780, 750, 690, 710, 680, 730, 690, 720, 740, 690, 610, 690, 710, 680, 770, 610, 580, 650, 540, 590, 620, 600, 550, 550, 570, 670, 660, 580, 650, 660, 640, 620, 660, 660, 680, 650, 670, 580, 590, 690 };
            //var gpa = new double[] { 4, 3.9, 3.3, 3.7, 3.9, 3.7, 2.3, 3.3, 3.3, 1.7, 2.7, 3.7, 3.7, 3.3, 3.3, 3, 2.7, 3.7, 2.7, 2.3, 3.3, 2, 2.3, 2.7, 3, 3.3, 3.7, 2.3, 3.7, 3.3, 3, 2.7, 4, 3.3, 3.3, 2.3, 2.7, 3.3, 1.7, 3.7 };
            //var work_experience = new double[] { 3, 4, 3, 5, 4, 6, 1, 4, 5, 1, 3, 5, 6, 4, 3, 1, 4, 6, 2, 3, 2, 1, 4, 1, 2, 6, 4, 2, 6, 5, 1, 2, 4, 6, 5, 1, 2, 1, 4, 5 };

            //var admitted = new double[] { 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1 };


            //IRI.Maptor.Sta.Mathematics.Matrix matrix = new IRI.Maptor.Sta.Mathematics.Matrix(gmat.Length, 3);

            //matrix.SetColumn(0, gmat);
            //matrix.SetColumn(1, gpa);
            //matrix.SetColumn(2, work_experience);

            LogisticRegression lr = new LogisticRegression(new LogisticRegressionOptions() { RegularizationMethod = RegularizationMethods.None });
            lr.Fit(_gmat_gpa_workExperience, _admitted);

            var newGmat = new double[] { 590, 740, 680, 610, 710 };
            var newGpa = new double[] { 2, 3.7, 3.3, 2.3, 3 };
            var newWork_experience = new double[] { 3, 4, 6, 1, 5 };

            Assert.Equal(0.0, Math.Round(lr.Predict(new List<double>() { newGmat[0], newGpa[0], newWork_experience[0] }).Value));
            Assert.Equal(1.0, Math.Round(lr.Predict(new List<double>() { newGmat[1], newGpa[1], newWork_experience[1] }).Value));
            Assert.Equal(1.0, Math.Round(lr.Predict(new List<double>() { newGmat[2], newGpa[2], newWork_experience[2] }).Value));
            Assert.Equal(0.0, Math.Round(lr.Predict(new List<double>() { newGmat[3], newGpa[3], newWork_experience[3] }).Value));
            Assert.Equal(1.0, Math.Round(lr.Predict(new List<double>() { newGmat[4], newGpa[4], newWork_experience[4] }).Value));
        }
    }
} 