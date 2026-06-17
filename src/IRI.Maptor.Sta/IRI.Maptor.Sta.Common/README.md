# IRI.Maptor.Sta.Common

The **IRI.Maptor.Sta.Common** project is the foundational building block of the **Maptor GIS library suite**. It provides core abstractions, utility classes, mathematical models, data structures, and essential helpers required by higher-level Maptor components. This library ensures consistency and reusability across all GIS-related modules.

---

## 🔍 Overview

This library focuses on:

* **Core interfaces and abstractions** for domain-driven design (e.g., `IIdentifiable`, `IPoint`)
* **Mathematical operations** including linear algebra and statistics
* **Advanced data structures** (trees, heaps, disjoint sets)
* **Unit and measurement conversions** (linear, angular)
* **Extensions and helpers** for strings, numbers, collections, and I/O
* **Service models** for integration with external APIs (Google, Bing, Here, Mapzen)

It acts as the **backbone for Maptor**, ensuring other projects share a unified and optimized base.

---

## ✨ Features

* **Abstractions** for geometric and domain models
* **Encodings** (Base64 URL, Persian DOS)
* **Enums and attribute utilities**
* **Rich set of data structures**: binary heaps, trees, interval trees, red-black trees, disjoint sets
* **Helpers** for I/O, JSON, XML, HTTP, security, randomness
* **Mathematics**: matrices, vectors, eigenvalues, chi-square, statistics models
* **Unit conversions** for angles and lengths (meters, feet, miles, etc.)
* **Extensions** for `string`, `DateTime`, `IEnumerable`, `int`, `double`, etc.
* **External service contracts** for routing, geolocation, and dictionary services

---

## 📚 Installation

Install via **NuGet**:

```powershell
Install-Package IRI.Maptor.Sta.Common
```

Targets **.NET Standard 2.1** — compatible with .NET 5+ and .NET Core 3.x.

---

## 🔄 Example Usage

### Working with Points and Bounding Boxes:

```csharp
using IRI.Maptor.Sta.Common.Primitives;

var pointA = new Point(10.5, 20.3);
var pointB = new Point(12.7, 22.1);

var bbox = new BoundingBox(pointA, pointB);
Console.WriteLine($"Bounding Box: {bbox}");
```

### Using Unit Conversions:

```csharp
using IRI.Maptor.Sta.Common.Units.Linear;

double meters = 1000;
var miles = new Mile(meters);
Console.WriteLine($"{meters} meters = {miles.Value} miles");
```

---

## 🛠 Project Structure

```
Common/
  Abstractions/        # Core interfaces (IHasM, IIdentifiable, IRepository, etc.)
  Attributes/          # Custom attributes like FieldAttribute
  Encodings/           # Encoding utilities (Base64Url, Persian DOS)
  Enums/               # Enum definitions for geometry and spatial relations
  Exceptions/          # Custom exceptions
  JsonConverters/      # JSON converters for specialized types
  Randoms/             # Random generators
Contracts/
  Google/, Bing/, Here/, Mapzen/ # API response models
DataStructures/
  BinaryHeap/, Trees/, SortAlgorithms/ # Advanced structures
Extensions/            # Extension methods for core types
Helpers/               # Utility classes for I/O, HTTP, security, etc.
Mathematics/           # Linear algebra, statistics, optimization
Models/                # Domain models (GeoreferencedImage, ValueObject)
Primitives/            # Geometric primitives (Point, LineSegment, BoundingBox)
Services/              # Service models and helpers (Oxford API, DateTime services)
Units/                 # Unit conversion systems (Linear, Angular)
```

---

## 💪 Why Use This Library?

* Provides **solid foundations** for building GIS and spatial applications
* Eliminates repetitive code with **rich utility support**
* Ensures **high performance** via optimized algorithms and structures

---

## 👥 Contributing

Contributions are welcome! Please fork the repository, create a feature branch, and submit a pull request.

---

## ⚖️ License

This project is licensed under the **MIT License**.
