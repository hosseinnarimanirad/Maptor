﻿// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Sta.Mathematics;
using IRI.Sta.DataStructures;
using System.Xml.Serialization;

namespace IRI.Ket.DigitalImageProcessing.ImageMatching;

[Serializable()]
public struct KeyPoint
{
    public int ExtermaIndex;

    public double Orientation;

    public double Magnitude;

    public KeyPoint(int extermaIndex, double orientation, double magnitude)
    {
        if (orientation < 0 || orientation > Math.PI * 2)
        {
            throw new NotImplementedException();
        }

        this.ExtermaIndex = extermaIndex;

        this.Orientation = orientation;

        this.Magnitude = magnitude;
    }
}
