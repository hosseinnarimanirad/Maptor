# Lat/long coordinate precision analysis

A practical guide to understanding how decimal precision in latitude/longitude coordinates translates to real-world distances.

## Why precision matters

The number of decimal places in geographic coordinates directly affects:
- **Accuracy** of location-based services
- **Storage efficiency** in databases
- **Computational performance** in spatial calculations

## Precision ↔ distance reference table
Mainly it depend on the latitude of point:

![image](https://github.com/user-attachments/assets/44af8d52-0e7a-48d8-a03d-fcfea450b784)

## Code example

```csharp
// Calculate precision at specific latitude
var basePoint = new Point(0, latitude);
var testPoint = new Point(0.00001, latitude); // 5 decimal places
double distance = basePoint.SphericalDistance(testPoint);

Console.WriteLine($"At latitude {latitude}°:");
Console.WriteLine($"0.00001° ≈ {distance:0.##} meters E/W");
```

## Key recommendations

    General purpose: 4 decimal places (~11m)
    Urban applications: 5 decimal places (~1m precision)
    Navigation systems: 6 decimal places (~0.1m)
    Survey engineering: 7+ decimal places
    

## Learn more

For detailed technical analysis:  
    [StackExchange: How precise should lat/long storage be?](https://gis.stackexchange.com/a/208739)

---
[Back to the solution README](../../../README.md)
