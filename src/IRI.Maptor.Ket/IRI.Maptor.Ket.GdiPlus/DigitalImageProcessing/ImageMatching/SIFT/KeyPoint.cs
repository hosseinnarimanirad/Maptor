// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Ket.DigitalImageProcessing.ImageMatching;

[Serializable()]
public struct KeyPoint
{
    public int ExtremumIndex;

    public double Orientation;

    public double Magnitude;

    public KeyPoint(int extremumIndex, double orientation, double magnitude)
    {
        if (orientation < 0 || orientation > Math.PI * 2)
        {
            throw new NotImplementedException();
        }

        this.ExtremumIndex = extremumIndex;

        this.Orientation = orientation;

        this.Magnitude = magnitude;
    }
}
