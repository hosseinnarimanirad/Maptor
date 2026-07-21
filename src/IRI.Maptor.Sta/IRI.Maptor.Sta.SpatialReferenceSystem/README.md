# 🌐 IRI.Maptor.Sta.SpatialReferenceSystem

**A .NET library for advanced spatial reference systems (SRS), geodetic transformations, and map projections**

This library provides implementations of horizontal spatial reference systems as defined in geodesy, supporting three primary categories:

- **Terrestrial Coordinate Systems**
  - Conventional Terrestrial (CT or AT)
  - Instantaneous Terrestrial (IT)
  - Geodetic (G)
  - Local Geodetic (LG)
  - Local Astronomic
- **Celestial Coordinate Systems**
  - Apparent Places (AP)
  - Right Ascension (RA)
  - Horizontal Angle (HA)
- **Orbital Coordinate Systems**
  - Orbital (OR)
    
<p align="center">
  <img src="images/coordinate-systems.png" alt="Relationship between geocentric, topocentric, and 2D coordinate systems" width="600">
</p>

*Fig. 1: Relationship between geocentric, topocentric, and 2D coordinate systems*

## 📚 Documentation

Illustrated guides live next to the code:

- [CoordinateSystems](CoordinateSystems/README.md) — the geocentric/topocentric systems of Fig. 1 (IT, CT, G, LA, LG, …) and how `Transformations` converts between them
- [MapProjections](MapProjections/README.md) — UTM, Web Mercator, and the other implemented projections
- [Models](Models/README.md) — reference ellipsoids and horizontal datums (`Ellipsoid`, `Ellipsoids`)

## ✨ Features

### 1. Supported Map Projections & Horizontal Datums

<p align="center">
  <img src="images/map-projections.png" alt="Cylindrical, conic, and azimuthal projection surfaces" width="600">
</p>

*Fig. 2: Projection surfaces — cylindrical, conic, and azimuthal*

This library implements standard map projections and geodetic datums, including:
- **Projections:**
  - Transverse Mercator (TM)
  - Universal Transverse Mercator (UTM)
  - Web Mercator (Auxiliary Sphere)
  - Cylindrical Equal-Area
  - Albers Equal-Area Conic (1- and 2-parallel variants)
  - Lambert Conformal Conic (1- and 2-parallel variants)
- **Horizontal Datums:**
  - 30+ predefined ellipsoids (WGS84, GRS80, Clarke 1866, etc.)
  - Custom ellipsoid support via semi-major/minor axis parameters

### 2. Coordinate System Transformations
Transformation between different spatial reference systems are available including:
- CT <-> IT
- CT <-> G
- G1 <-> G2 (transform Geodetic system with different ellipsoids)
- CT <-> LA
- G <-> LG
- LA <-> LG
- LA <-> HA
- HA <-> AP
- IT <-> AP
- OR <-> AP

## 🚀 Getting Started

### Basic Usage

Convert WGS84 to AT:

```csharp
// Arrange
var wgs84Ellipsoid = Ellipsoids.WGS84;
double latitudeInDegrees = 35.123456;
double longitudeInDegrees = 51.123456;
    
var originalGeodeticPoint = new IRI.Maptor.Sta.Common.Primitives.Point(longitudeInDegrees, latitudeInDegrees);

// Act - Test geodetic to Cartesian conversion
var cartesianFromTransform = Transformations.ToCartesian(originalGeodeticPoint, wgs84Ellipsoid);

var cartesianFromGeodeticPoint = new GeodeticPoint<Meter, Degree>(
    wgs84Ellipsoid, 
    new Meter(0),
    new Degree(longitudeInDegrees),
    new Degree(latitudeInDegrees))
    .ToCartesian<Meter>();

// Assert - Both Cartesian conversion methods should produce same result
Assert.Equal(cartesianFromGeodeticPoint.X.Value, cartesianFromTransform.X, 9);
Assert.Equal(cartesianFromGeodeticPoint.Y.Value, cartesianFromTransform.Y, 9);
Assert.Equal(cartesianFromGeodeticPoint.Z.Value, cartesianFromTransform.Z, 9);
```

---

📦 **NuGet**: [IRI.Maptor.Sta.SpatialReferenceSystem](https://www.nuget.org/packages/IRI.Maptor.Sta.SpatialReferenceSystem)

🐞 **Report Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)