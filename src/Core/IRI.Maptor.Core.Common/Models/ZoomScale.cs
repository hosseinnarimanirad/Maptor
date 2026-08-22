using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Model;

public class ZoomScale
{
    public int ZoomLevel { get; private set; }

    //WebMercator scale
    public double Scale { get { return 1.0 / InverseScale; } }

    public double InverseScale { get; private set; }

    public ZoomScale(int zoomLevel, double inverseScale)
    {
        this.ZoomLevel = zoomLevel;

        this.InverseScale = inverseScale;
    }

    //WebMercator scale
    public double GetScaleAt(double latitude)
    {
        return Math.Cos(latitude * Math.PI / 180.0) * Scale;
    }

    public override string ToString() => $"Level: {ZoomLevel}; InverseScale: {InverseScale}";
}
