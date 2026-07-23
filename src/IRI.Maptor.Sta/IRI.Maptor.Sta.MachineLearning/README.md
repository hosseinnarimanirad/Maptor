# IRI.Maptor.Sta.MachineLearning

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.MachineLearning?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Machine learning algorithms and statistical tools used in the Maptor stack, with a focus on spatial data analysis — clustering, association rule mining, logistic regression, and descriptive statistics.

## Installation

```bash
dotnet add package IRI.Maptor.Sta.MachineLearning
```

## Features

- DBSCAN density-based clustering (`Dbscan.Cluster<T>`) with a caller-supplied distance function
- Apriori frequent-itemset mining (`AprioriAlgorithm`, `Itemset`)
- Binary logistic regression (`LogisticRegression`) with configurable options: feature normalization, regularization method, variance calculation mode
- Logistic-regression-based line simplification use case (`LogisticSimplification` with synthetic training data helpers)
- Descriptive statistics (`GeneralStatistics.CalculateSummary`)
- Shared building blocks: sigmoid, normalization, and regularization helpers

## Usage

Train and use a logistic regression classifier:

```csharp
using IRI.Maptor.Sta.MachineLearning;
using IRI.Maptor.Sta.Mathematics;

var options = new LogisticRegressionOptions { NormalizeFeatures = true };
var lr = new LogisticRegression(options);

// each row of xValues is one observation; yValues holds the 0/1 labels
lr.Fit(xValues, yValues);   // xValues: Matrix, yValues: double[]

double? probability = lr.Predict(new List<double> { 2.5, 1.3 });
```

Summarize a data series:

```csharp
using IRI.Maptor.Sta.MachineLearning;

var summary = GeneralStatistics.CalculateSummary(new double[] { 1, 2, 3, 4, 5 });
```

## Limitations

- `Dbscan.Cluster` currently returns `void`; the computed cluster assignments are not yet exposed to the caller.
- Logistic regression is binary-only (no multi-class support).

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Sta](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/README.md)
