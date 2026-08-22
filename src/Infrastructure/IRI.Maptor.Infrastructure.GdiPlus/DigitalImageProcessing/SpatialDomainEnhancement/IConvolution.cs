// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Infrastructure.GdiPlus.DigitalImageProcessing;

public interface IConvolution
{
    double Sigma{get;}

    Matrix Convolve(Matrix original);

}
