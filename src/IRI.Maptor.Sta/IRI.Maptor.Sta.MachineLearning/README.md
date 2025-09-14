# 🤖 IRI.Maptor.Sta.MachineLearning



\[!\[.NET Standard](https://img.shields.io/badge/.NET-Standard2.1-blue.svg)](https://dotnet.microsoft.com/)

\[!\[License](https://img.shields.io/github/license/hosseinnarimanirad/Maptor)](LICENSE)



IRI.Maptor.Sta.MachineLearning is a lightweight .NET library for integrating machine learning algorithms into .NET applications.  

It provides a simple API for training, evaluating, and predicting with classic ML algorithms.



---



## ✨ Features



\- 🚀 Easy-to-use API for ML workflows

\- 📊 Built-in support for \*\*Classification\*\*, \*\*Regression\*\*, and \*\*Clustering\*\*

\- 🔧 Data preprocessing utilities (normalization, scaling, encoding)

\- 📈 Evaluation metrics (accuracy, RMSE, confusion matrix)

\- 🧩 Extensible design for plugging in new algorithms

\- ⚡ Optimized for performance on .NET



---



## ⚙️ Installation



```bash

dotnet add package IRI.Maptor.Sta.MachineLearning

```



---



## 💻 Usage Examples



### Example 1 – Training a Classification Model

```csharp

using IRI.Maptor.Sta.MachineLearning;

using IRI.Maptor.Sta.MachineLearning.Models;

using IRI.Maptor.Sta.MachineLearning.Data;



// Load training data

var data = DataTableLoader.Load("data.csv");



// Create and train model

var model = new DecisionTreeClassifier();

model.Train(data.Features, data.Labels);



// Save model

model.Save("decision\_tree.model");

```



---



### Example 2 – Making Predictions

```csharp

var loadedModel = DecisionTreeClassifier.Load("decision\_tree.model");



double\[] newSample = { 5.1, 3.5, 1.4, 0.2 };

var prediction = loadedModel.Predict(newSample);



Console.WriteLine($"Predicted class: {prediction}");

```



---



### Example 3 – Evaluating Model Performance

```csharp

var testData = DataTableLoader.Load("test.csv");



var predictions = model.Predict(testData.Features);

var accuracy = Metrics.Accuracy(testData.Labels, predictions);



Console.WriteLine($"Accuracy: {accuracy:P2}");

```



---



## 📂 Project Structure

```

IRI.Maptor.Sta.MachineLearning/

│

├── Data/              # Data loading and preprocessing utilities

├── Models/            # ML algorithms (Classification, Regression, Clustering)

├── Metrics/           # Evaluation metrics

├── Core/              # Base ML abstractions

└── README.md          # Documentation

```



---



## 🤝 Contributing

Contributions are welcome!  

Please include tests and documentation updates when submitting pull requests.



---



## 📜 License

This project is licensed under the [MIT License](LICENSE).



