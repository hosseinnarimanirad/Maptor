# IRI.Maptor.Sta.MachineLearning

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.MachineLearning.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

A .NET Standard 2.1 library of machine learning algorithms and statistical tools designed for spatial and geospatial data analysis.

---

## Algorithms

### Clustering
- **DBSCAN** (`DbScan`) — density-based spatial clustering; well suited for geographic point data with noise/outliers

### Association Rule Mining
- **Apriori** (`AprioriAlgorithm`, `Itemset`) — frequent itemset mining and association rule generation

### Classification / Regression
- **Logistic Regression** (`LogisticRegression`, `LogisticRegressionHelper`, `LogisticRegressionOptions`) — binary classification with configurable training options

### Statistics
- **General statistics** (`GeneralStatistics`) — descriptive statistics helpers used across spatial analysis

---

## Installation

```bash
dotnet add package IRI.Maptor.Sta.MachineLearning
```

---

## Project Structure

```
Sta.MachineLearning/
├── Clustering/
│   └── DbScan.cs
├── Apriori/
│   ├── AprioriAlgorithm.cs
│   └── Itemset.cs
├── LogisticRegression/
│   ├── LogisticRegression.cs
│   ├── LogisticRegressionHelper.cs
│   ├── LogisticRegressionOptions.cs
│   └── UseCases/
├── GeneralStatistics.cs
├── Common/
└── Extensions/
```

---

📦 **NuGet**: [IRI.Maptor.Sta.MachineLearning](https://www.nuget.org/packages/IRI.Maptor.Sta.MachineLearning)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)
