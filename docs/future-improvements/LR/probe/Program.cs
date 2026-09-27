// LrProbe: reproduces the findings in ../logistic-regression-bugs.md against the live library code.
//
//   dotnet run -c Release -- basic     unit-test dataset (40 rows): replication, L1 sign, std mismatch, mutation, NRE
//   dotnet run -c Release -- usecase   synthetic simplification training set (4,182 records), LR4 / LR7 / ALL13
//   dotnet run -c Release -- all       both (default)
//
// Nothing here modifies the repository. "Replica" re-implements LogisticRegression.Fit step by step and must
// report max|diff| = 0 against the library; if it does not, the library changed and this probe needs updating.

using IRI.Maptor.Core.MachineLearning;
using IRI.Maptor.Core.Common.Mathematics;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Spatial.Primitives;
using System.Globalization;
using System.Reflection;
using F = IRI.Maptor.Core.MachineLearning.LRSimplificationFeatures;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
if (mode is "basic" or "all") BasicProbes.Run();
if (mode is "usecase" or "all") UseCaseProbes.Run();

// =====================================================================================================
// Part 1: the unit-test dataset (tests/IRI.Maptor.Tests/MachineLearning/LogisticRegressionTest.cs)
// =====================================================================================================
static class BasicProbes
{
    static readonly double[] gmat = { 780, 750, 690, 710, 680, 730, 690, 720, 740, 690, 610, 690, 710, 680, 770, 610, 580, 650, 540, 590, 620, 600, 550, 550, 570, 670, 660, 580, 650, 660, 640, 620, 660, 660, 680, 650, 670, 580, 590, 690 };
    static readonly double[] gpa = { 4, 3.9, 3.3, 3.7, 3.9, 3.7, 2.3, 3.3, 3.3, 1.7, 2.7, 3.7, 3.7, 3.3, 3.3, 3, 2.7, 3.7, 2.7, 2.3, 3.3, 2, 2.3, 2.7, 3, 3.3, 3.7, 2.3, 3.7, 3.3, 3, 2.7, 4, 3.3, 3.3, 2.3, 2.7, 3.3, 1.7, 3.7 };
    static readonly double[] work = { 3, 4, 3, 5, 4, 6, 1, 4, 5, 1, 3, 5, 6, 4, 3, 1, 4, 6, 2, 3, 2, 1, 4, 1, 2, 6, 4, 2, 6, 5, 1, 2, 4, 6, 5, 1, 2, 1, 4, 5 };
    static readonly double[] admitted = { 1, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1 };

    static Matrix Build(int reps, double gmatSign = 1)
    {
        int m = gmat.Length * reps;
        var mat = new Matrix(m, 3);
        var c0 = new double[m]; var c1 = new double[m]; var c2 = new double[m];
        for (int r = 0; r < reps; r++)
            for (int i = 0; i < gmat.Length; i++)
            { c0[r * gmat.Length + i] = gmatSign * gmat[i]; c1[r * gmat.Length + i] = gpa[i]; c2[r * gmat.Length + i] = work[i]; }
        mat.SetColumn(0, c0); mat.SetColumn(1, c1); mat.SetColumn(2, c2);
        return mat;
    }

    static double[] Y(int reps) => Enumerable.Range(0, reps).SelectMany(_ => admitted).ToArray();

    static string B(double[] b) => string.Join(", ", b.Select(v => v.ToString("F4")));

    // mean-gradient norm of the log-loss at the fitted beta, using POPULATION-std normalization (what Fit used)
    static double GradNorm(LogisticRegression lr, Matrix x, double[] y)
    {
        int m = x.NumberOfRows, p = x.NumberOfColumns;
        var mean = new double[p]; var std = new double[p];
        for (int j = 0; j < p; j++)
        {
            var col = x.GetColumn(j);
            mean[j] = col.Average();
            std[j] = Math.Sqrt(col.Select(v => (v - mean[j]) * (v - mean[j])).Sum() / m);
        }
        var g = new double[p + 1];
        for (int i = 0; i < m; i++)
        {
            var z = lr.Beta[0];
            var xs = new double[p + 1]; xs[0] = 1;
            for (int j = 0; j < p; j++) { xs[j + 1] = (x[i, j] - mean[j]) / std[j]; z += lr.Beta[j + 1] * xs[j + 1]; }
            var e = 1.0 / (1.0 + Math.Exp(-z)) - y[i];
            for (int j = 0; j <= p; j++) g[j] += e * xs[j];
        }
        return Math.Sqrt(g.Sum(v => v * v)) / m;
    }

    public static void Run()
    {
        Console.WriteLine("=== A. baseline m=40, None ===");
        {
            var lr = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None });
            lr.Fit(Build(1), Y(1));
            Console.WriteLine($"beta = {B(lr.Beta)}   meanGradNorm = {GradNorm(lr, Build(1), Y(1)):E2}");
        }

        Console.WriteLine("=== B. same data replicated (identical optimum), None  [bug #1] ===");
        foreach (var reps in new[] { 1, 5, 25, 100, 250 })
        {
            var lr = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None });
            lr.Fit(Build(reps), Y(reps));
            Console.WriteLine($"m={gmat.Length * reps,5}: beta = {B(lr.Beta)}   meanGradNorm = {GradNorm(lr, Build(reps), Y(reps)):E2}");
        }

        Console.WriteLine("=== C. negate the gmat column (expect beta[1] to flip sign only)  [bug #2] ===");
        {
            var lr1 = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.L1 }); lr1.Fit(Build(1), Y(1));
            var lr2 = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.L1 }); lr2.Fit(Build(1, -1), Y(1));
            Console.WriteLine($"L1  gmat: beta = {B(lr1.Beta)}");
            Console.WriteLine($"L1 -gmat: beta = {B(lr2.Beta)}");
            var lr3 = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.L2 }); lr3.Fit(Build(1), Y(1));
            var lr4 = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.L2 }); lr4.Fit(Build(1, -1), Y(1));
            Console.WriteLine($"L2  gmat: beta = {B(lr3.Beta)}");
            Console.WriteLine($"L2 -gmat: beta = {B(lr4.Beta)}");
        }

        Console.WriteLine("=== D. train/predict normalization mismatch (Population at fit, stored stats use Sample)  [bug #3] ===");
        {
            var lr = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None });
            lr.Fit(Build(1), Y(1));
            var mean = gmat.Average();
            var popStd = Math.Sqrt(gmat.Select(v => (v - mean) * (v - mean)).Sum() / gmat.Length);
            var samStd = Math.Sqrt(gmat.Select(v => (v - mean) * (v - mean)).Sum() / (gmat.Length - 1));
            Console.WriteLine($"gmat std: population={popStd:F4} sample={samStd:F4} stored in xStatistics={lr.xStatistics[1].StandardDeviation:F4}");
            double zFit = lr.Beta[0];
            var cols = new[] { gmat, gpa, work };
            for (int j = 0; j < 3; j++) { var mu = cols[j].Average(); var sd = Math.Sqrt(cols[j].Select(v => (v - mu) * (v - mu)).Sum() / cols[j].Length); zFit += lr.Beta[j + 1] * (cols[j][0] - mu) / sd; }
            var pFit = 1.0 / (1.0 + Math.Exp(-zFit));
            var pPred = lr.Predict(new List<double> { gmat[0], gpa[0], work[0] }).Value;
            Console.WriteLine($"row0: p(as fitted)={pFit:F6}  p(Predict)={pPred:F6}");

            var lr2 = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None });
            var x3 = new Matrix(3, 1); x3.SetColumn(0, new double[] { 1, 2, 4 }); lr2.Fit(x3, new double[] { 0, 0, 1 });
            var mu3 = 7 / 3.0;
            var pop3 = Math.Sqrt(((1 - mu3) * (1 - mu3) + (2 - mu3) * (2 - mu3) + (4 - mu3) * (4 - mu3)) / 3);
            Console.WriteLine($"n=3: stored std={lr2.xStatistics[1].StandardDeviation:F4} (population would be {pop3:F4})  Predict(4)={lr2.Predict(new List<double> { 4 }):F4}");
        }

        Console.WriteLine("=== E. NormalizeFeatures=false: Fit mutates caller's matrix  [bug #4] ===");
        {
            var x = Build(1);
            Console.WriteLine($"columns before Fit: {x.NumberOfColumns}");
            var lr = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None, NormalizeFeatures = false });
            lr.Fit(x, Y(1));
            Console.WriteLine($"columns after  Fit: {x.NumberOfColumns}   beta.Length={lr.Beta.Length}");
            var lr2 = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None, NormalizeFeatures = false });
            lr2.Fit(x, Y(1));
            Console.WriteLine($"columns after 2nd Fit: {x.NumberOfColumns}   beta.Length={lr2.Beta.Length}");
            try { var p = lr2.Predict(new List<double> { 590, 2, 3 }); Console.WriteLine($"Predict after 2nd fit = {p}"); }
            catch (Exception ex) { Console.WriteLine($"Predict after 2nd fit threw {ex.GetType().Name}: {ex.Message}"); }
        }

        Console.WriteLine("=== F. Predict mutates caller's list  [bug #5] ===");
        {
            var lr = new LogisticRegression(new LogisticRegressionOptions { RegularizationMethod = RegularizationMethods.None });
            lr.Fit(Build(1), Y(1));
            var input = new List<double> { 590, 2, 3 };
            var p1 = lr.Predict(input);
            Console.WriteLine($"after 1st Predict: count={input.Count} values=[{string.Join(", ", input.Select(v => v.ToString("F3")))}] p={p1:F4}");
            try { var p2 = lr.Predict(input); Console.WriteLine($"2nd Predict on same list: p={p2:F4}"); }
            catch (Exception ex) { Console.WriteLine($"2nd Predict on same list threw {ex.GetType().Name}: {ex.Message}"); }
        }

        Console.WriteLine("=== G. ctor(beta, stats) then Fit  [bug #6] ===");
        {
            var lr = new LogisticRegression(new double[] { 0, 1, 1, 1 }, null);
            try { lr.Fit(Build(1), Y(1)); Console.WriteLine("ok"); }
            catch (Exception ex) { Console.WriteLine($"threw {ex.GetType().Name}"); }
        }
        Console.WriteLine();
    }
}

// =====================================================================================================
// Part 2: the simplification use case, trained the way research/IRI.Maptor.Res.LRSimplification does it
// =====================================================================================================
static class UseCaseProbes
{
    static readonly Dictionary<string, List<F>> FeatureSets = new()
    {
        ["LR4"] = new() { F.DistanceToNext, F.DistanceToPrevious, F.Area, F.SquareCosineOfAngle },
        ["LR7"] = new() { F.DistanceToNext, F.DistanceToPrevious, F.Area, F.SquareCosineOfAngle, F.VerticalDistance, F.CosineOfAngle, F.BaseLength },
        ["ALL13"] = new() { F.Area, F.BaseLength, F.CosineOfAngle, F.DistanceToNext, F.DistanceToPrevious, F.SquareCosineOfAngle, F.VerticalDistance, F.dX12, F.dX13, F.dX23, F.dY12, F.dY13, F.dY23 },
    };

    static readonly FieldInfo RegField = typeof(LogisticSimplification<Point>).GetField("_regression", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void Run()
    {
        var testLines = MakeTestPolylines(300);

        foreach (var (name, features) in FeatureSets)
        {
            Console.WriteLine($"################ {name} ({features.Count} features) ################");

            // ---- 1. training set, built exactly like the research tool (SyntheticDataFactory.CreateAll -> LRSimplificationTrainingData.Create)
            var records = new List<LRSimplificationParameters<Point>>();
            foreach (var item in SyntheticDataFactory.CreateAll())
            {
                var orig = Geometry<Point>.FromWkt(item.OriginalLineString, 0);
                var simp = Geometry<Point>.FromWkt(item.SimplifiedLineString, 0);
                var td = LRSimplificationTrainingData<Point>.Create(orig.GetLastPart(), simp.GetLastPart(), orig.IsRingBase(), features);
                foreach (var r in td.Records) r.Features = features;
                records.AddRange(td.Records);
            }
            var trainingData = new LRSimplificationTrainingData<Point>(records);
            int m = records.Count, p = features.Count;
            double[][] X = records.Select(r => r.FeatureValues.ToArray()).ToArray();
            double[] y = records.Select(r => r.IsRetained ? 1.0 : 0.0).ToArray();
            int nan = X.Count(row => row.Any(double.IsNaN));
            Console.WriteLine($"records={m}, retained={y.Sum():F0}, rows with NaN features={nan}");

            var mean = new double[p]; var popStd = new double[p]; var samStd = new double[p];
            for (int j = 0; j < p; j++)
            {
                mean[j] = X.Average(r => r[j]);
                var ss = X.Sum(r => (r[j] - mean[j]) * (r[j] - mean[j]));
                popStd[j] = Math.Sqrt(ss / m); if (popStd[j] == 0) popStd[j] = 1;
                samStd[j] = Math.Sqrt(ss / (m - 1)); if (samStd[j] == 0) samStd[j] = 1;
            }
            double[][] Z = X.Select(r => { var z = new double[p + 1]; z[0] = 1; for (int j = 0; j < p; j++) z[j + 1] = (r[j] - mean[j]) / popStd[j]; return z; }).ToArray();

            // ---- 2. current library model (what LogisticSimplification.Create does today)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var modelA = LogisticSimplification<Point>.Create(trainingData, "A");
            sw.Stop();
            var regA = (LogisticRegression)RegField.GetValue(modelA);
            var betaA = regA.Beta;
            Console.WriteLine($"[A: current code]  fit {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"   beta = {Fmt(betaA)}");
            Console.WriteLine($"   mean-gradient norm at beta = {GradNorm(Z, y, betaA):E2}   (converged fit would be ~1e-8 or less)");
            Console.WriteLine($"   stored std (Sample) / population std, feature 1: {regA.xStatistics[1].StandardDeviation:F4} / {popStd[0]:F4}");

            // ---- 3. replica of the library's loop: proves the model of the code is exact, and shows the last-iteration jump
            var betaRep = Replica(Z, y, scaleByRows: false, l1Sign: false, iters: 5000, lr: 0.1);
            var betaRep4999 = Replica(Z, y, scaleByRows: false, l1Sign: false, iters: 4999, lr: 0.1);
            Console.WriteLine($"   replica(5000) max|diff| vs library = {betaA.Zip(betaRep, (a, b) => Math.Abs(a - b)).Max():E2}   (must be 0)");
            Console.WriteLine($"   beta change in the LAST iteration (4999->5000): max|diff| = {betaRep.Zip(betaRep4999, (a, b) => Math.Abs(a - b)).Max():E2}");

            // ---- 4. converged reference fits
            //    C1: only fix #1 (scale by rows), L1 still without sign        -> isolates the convergence fix
            //    C2: fix #1 + fix #2 (L1 with sign)                             -> full corrected objective
            var betaC1 = Converge(Z, y, l1Sign: false, out var g1, out var it1);
            var betaC2 = Converge(Z, y, l1Sign: true, out var g2, out var it2);
            Console.WriteLine($"[C1: fix #1 only]  beta = {Fmt(betaC1)}   grad={g1:E1} after {it1} it");
            Console.WriteLine($"[C2: fix #1 + #2]  beta = {Fmt(betaC2)}   grad={g2:E1} after {it2} it");
            Console.WriteLine($"   coefficients with negative sign in C2: {betaC2.Skip(1).Count(b => b < 0)} of {p}");

            // ---- 5. decisions on the training records (retain if p > 0.5)
            bool[] decA = records.Select(r => ModelDecision(regA, r)).ToArray();          // production path (library Predict, sample std)
            bool[] decA_pop = Z.Select(z => Dot(z, betaA) > 0).ToArray();                 // same beta, population std (fix #3 only)
            bool[] decC1 = Z.Select(z => Dot(z, betaC1) > 0).ToArray();
            bool[] decC2 = Z.Select(z => Dot(z, betaC2) > 0).ToArray();
            bool[] label = y.Select(v => v > 0.5).ToArray();
            Console.WriteLine($"   training accuracy: A={Acc(decA, label):P2}  A+fix3={Acc(decA_pop, label):P2}  C1={Acc(decC1, label):P2}  C2={Acc(decC2, label):P2}");
            Console.WriteLine($"   records whose decision differs from A: fix#3 only={Diff(decA, decA_pop)}  fix#1={Diff(decA, decC1)}  fix#1+#2={Diff(decA, decC2)}  (fix#2 alone on top of #1: {Diff(decC1, decC2)})");

            // ---- 6. actual simplification output on test polylines, current model vs models with corrected betas injected
            var modelA_pop = Inject(trainingData, betaA, mean, popStd);   // fix #3 only
            var modelC1 = Inject(trainingData, betaC1, mean, popStd);
            var modelC2 = Inject(trainingData, betaC2, mean, popStd);
            CompareSimplification("fix #3 only ", modelA, modelA_pop, testLines);
            CompareSimplification("fix #1      ", modelA, modelC1, testLines);
            CompareSimplification("fix #1 + #2 ", modelA, modelC2, testLines);
            Console.WriteLine();
        }
    }

    static string Fmt(double[] b) => string.Join(", ", b.Select(v => v.ToString("F3")));
    static double Dot(double[] a, double[] b) { double s = 0; for (int i = 0; i < a.Length; i++) s += a[i] * b[i]; return s; }
    static double Sig(double z) => 1.0 / (1.0 + Math.Exp(-z));
    static double Acc(bool[] a, bool[] b) => a.Zip(b, (x, y) => x == y ? 1.0 : 0.0).Average();
    static int Diff(bool[] a, bool[] b) => a.Zip(b, (x, y) => x != y ? 1 : 0).Sum();

    static bool ModelDecision(LogisticRegression reg, LRSimplificationParameters<Point> r)
    {
        var v = reg.Predict(new List<double>(r.FeatureValues));
        return v.HasValue && v.Value > 0.5;
    }

    static double GradNorm(double[][] Z, double[] y, double[] beta)
    {
        var g = new double[beta.Length];
        for (int i = 0; i < Z.Length; i++) { var e = Sig(Dot(Z[i], beta)) - y[i]; for (int k = 0; k < g.Length; k++) g[k] += e * Z[i][k]; }
        return Math.Sqrt(g.Sum(v => v * v)) / Z.Length;
    }

    // mirrors LogisticRegression.Fit: beta0 = 1, grad = 1/L * sum + lambda/L (L1 without sign), lr 0.1, L = number of parameters
    static double[] Replica(double[][] Z, double[] y, bool scaleByRows, bool l1Sign, int iters, double lr)
    {
        int L = Z[0].Length; var beta = Enumerable.Repeat(1.0, L).ToArray();
        double lambda = 1.0;
        double scale = scaleByRows ? 1.0 / Z.Length : 1.0 / L;
        for (int it = 0; it < iters; it++)
        {
            var g = new double[L];
            for (int i = 0; i < Z.Length; i++) { var e = Sig(Dot(Z[i], beta)) - y[i]; for (int k = 0; k < L; k++) g[k] += e * Z[i][k]; }
            for (int k = 0; k < L; k++)
            {
                double reg = k == 0 ? 0 : (l1Sign ? Math.Sign(beta[k]) : 1.0) * lambda * scale;
                g[k] = scale * g[k] + reg;
                beta[k] -= lr * g[k];
            }
        }
        return beta;
    }

    // converged fit of: sum logloss + lambda * L1(beta[1..])   (the objective the library encodes; lambda = 1)
    static double[] Converge(double[][] Z, double[] y, bool l1Sign, out double gradNorm, out int iterations)
    {
        int L = Z[0].Length; int m = Z.Length; var beta = Enumerable.Repeat(1.0, L).ToArray();
        double lambda = 1.0, lr = 0.5; gradNorm = double.MaxValue; iterations = 0;
        var vel = new double[L]; // heavy-ball momentum, helps the ill-conditioned (correlated) feature sets
        for (int it = 0; it < 400000; it++)
        {
            var g = new double[L];
            for (int i = 0; i < m; i++) { var e = Sig(Dot(Z[i], beta)) - y[i]; for (int k = 0; k < L; k++) g[k] += e * Z[i][k]; }
            for (int k = 1; k < L; k++) g[k] += (l1Sign ? Math.Sign(beta[k]) : 1.0) * lambda;
            for (int k = 0; k < L; k++) g[k] /= m;
            gradNorm = Math.Sqrt(g.Sum(v => v * v));
            iterations = it + 1;
            if (gradNorm < 1e-9) break;
            for (int k = 0; k < L; k++) { vel[k] = 0.9 * vel[k] - lr * g[k]; beta[k] += vel[k]; }
        }
        return beta;
    }

    // a LogisticSimplification whose regression is replaced by (beta, stats) through reflection
    static LogisticSimplification<Point> Inject(LRSimplificationTrainingData<Point> td, double[] beta, double[] mean, double[] std)
    {
        var model = LogisticSimplification<Point>.Create(td, "injected");   // sets _features; we then replace _regression
        var stats = new List<BasicStatisticsInfo> { new BasicStatisticsInfo { Mean = 1, StandardDeviation = 0 } };
        for (int j = 0; j < mean.Length; j++) stats.Add(new BasicStatisticsInfo { Mean = mean[j], StandardDeviation = std[j] });
        var reg = new LogisticRegression((double[])beta.Clone(), stats);
        RegField.SetValue(model, reg);
        return model;
    }

    static void CompareSimplification(string label, LogisticSimplification<Point> a, LogisticSimplification<Point> b, List<List<Point>> lines)
    {
        int identical = 0, totalA = 0, totalB = 0, totalIn = 0;
        foreach (var line in lines)
        {
            var ra = a.SimplifyByLogisticRegression(line, pt => pt);
            var rb = b.SimplifyByLogisticRegression(line, pt => pt);
            totalIn += line.Count; totalA += ra.Count; totalB += rb.Count;
            if (ra.Count == rb.Count && ra.Zip(rb, (x, z) => x.X == z.X && x.Y == z.Y).All(t => t)) identical++;
        }
        Console.WriteLine($"   simplification [{label}]: identical output on {identical}/{lines.Count} polylines; points kept current={totalA} vs fixed={totalB} (input {totalIn})");
    }

    static List<List<Point>> MakeTestPolylines(int n)
    {
        var rnd = new Random(12345); var result = new List<List<Point>>();
        for (int i = 0; i < n; i++)
        {
            var pts = new List<Point>();
            int count = rnd.Next(20, 80);
            if (i % 2 == 0)
            {   // random walk in screen space, step 1..20 px, mild heading noise
                double x = 100, yv = 100, heading = rnd.NextDouble() * Math.PI * 2;
                for (int k = 0; k < count; k++)
                {
                    pts.Add(new Point(x, yv));
                    heading += (rnd.NextDouble() - 0.5) * 0.8;
                    var step = 1 + rnd.NextDouble() * 19;
                    x += Math.Cos(heading) * step; yv += Math.Sin(heading) * step;
                }
            }
            else
            {   // sine wave, amplitude 0.3..30 px, wavelength 10..200 px
                double amp = 0.3 + rnd.NextDouble() * 29.7, wl = 10 + rnd.NextDouble() * 190, dx = 1 + rnd.NextDouble() * 9;
                for (int k = 0; k < count; k++) pts.Add(new Point(100 + k * dx, 100 + amp * Math.Sin(2 * Math.PI * k * dx / wl)));
            }
            result.Add(pts);
        }
        return result;
    }
}
