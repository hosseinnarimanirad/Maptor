// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using IRI.Maptor.Core.Common.Metrics;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.SpatialReferenceSystem;

public class Ellipsoidal<TLinear, TAngular> : IEllipsoidal
    where TLinear : LinearUnit, new()
    where TAngular : AngularUnit, new()
{
    #region Fields

    private string _name;

    private IEllipsoid _datum;

    private AxisType _handedness;

    private const int _dimension = 2;

    private IAngularCollection _verticalAngle;

    private IAngularCollection _horizontalAngle;

    #endregion

    #region Properties

    public string Name
    {
        get { return this._name; }
    }

    public int Dimension
    {
        get { return _dimension; }
    }

    public IEllipsoid Datum
    {
        get { return this._datum; }
    }

    public int NumberOfPoints
    {
        get { return this.HorizontalAngle.Length; }
    }

    public AxisType Handedness
    {
        get { return this._handedness; }
    }

    public AngleMode AngularMode
    {
        get { return this.VerticalAngle.Mode; }
    }

    public AngleRange HorizontalRange
    {
        get { return this.VerticalAngle.Range; }
        set { this.VerticalAngle.Range = value; }
    }

    public IAngularCollection VerticalAngle
    {
        get { return this._verticalAngle; }
    }

    public IAngularCollection HorizontalAngle
    {
        get { return this._horizontalAngle; }
    }

    #endregion

    #region Constructors&Indexers

    public IEllipsoidalPoint this[int index]
    {
        get { return new EllipsoidalPoint<TLinear, TAngular>(this.Datum, this.HorizontalAngle[index], this.VerticalAngle[index]); }

        set
        {
            if (this.Datum != value.Datum)
            {
                throw new NotImplementedException();
            }

            this.HorizontalAngle[index] = value.HorizontalAngle;

            this.VerticalAngle[index] = value.VerticalAngle;
        }
    }

    public Ellipsoidal(IAngularCollection horizontalAngle, IAngularCollection verticalAngle, IEllipsoid ellipsoid)
        : this(horizontalAngle, verticalAngle, ellipsoid, AxisType.RightHanded) { }

    public Ellipsoidal(IAngularCollection horizontalAngle, IAngularCollection verticalAngle, IEllipsoid ellipsoid, AxisType handedness)
        : this("Geodetic", horizontalAngle, verticalAngle, ellipsoid, handedness) { }

    public Ellipsoidal(string name, IAngularCollection horizontalAngle, IAngularCollection verticalAngle, IEllipsoid ellipsoid, AxisType handedness)
    {
        if (horizontalAngle.Length != verticalAngle.Length)
        {
            throw new NotImplementedException();
        }

        this._name = name;

        this._handedness = handedness;

        this._datum = ellipsoid.ChangeTo<TLinear, TAngular>();

        this._horizontalAngle = (AngularCollection<TAngular>)horizontalAngle.ChangeTo<TAngular>();

        this._verticalAngle = (AngularCollection<TAngular>)verticalAngle.ChangeTo<TAngular>();
    }

    public Ellipsoidal(string name, Matrix values, AxisType handedness, AngleRange horizontalRange, IEllipsoid ellipsoid)
    {
        if (values.NumberOfColumns != this.Dimension)
        {
            throw new NotImplementedException();
        }

        this._name = name;

        this._handedness = handedness;

        this._datum = ellipsoid.ChangeTo<TLinear, TAngular>();

        this._horizontalAngle = new AngularCollection<TAngular>(values.GetColumn(0), horizontalRange);

        this._verticalAngle = new AngularCollection<TAngular>(values.GetColumn(1), AngleRange.MinusPIToPI);
    }

    #endregion

    #region Methods

    public IEllipsoidal RotateAboutX(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().RotateAboutX(value, direction);

        return temp.ToEllipsoidalForm<TLinear, TAngular>(this.Datum, this.HorizontalRange);
    }

    public IEllipsoidal RotateAboutY(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().RotateAboutY(value, direction);

        return temp.ToEllipsoidalForm<TLinear, TAngular>(this.Datum, this.HorizontalRange);
    }

    public IEllipsoidal RotateAboutZ(AngularUnit value, RotateDirection direction)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().RotateAboutZ(value, direction);

        return temp.ToEllipsoidalForm<TLinear, TAngular>(this.Datum, this.HorizontalRange);
    }

    public IEllipsoidal Shift(ISphericalPoint newBase)
    {
        Cartesian3D<TLinear> temp = (Cartesian3D<TLinear>)this.ToCartesian<TLinear>().Shift(newBase.ToCartesian<TLinear>());

        return temp.ToEllipsoidalForm<TLinear, TAngular>(this.Datum, this.HorizontalRange);
    }

    public Matrix ToMatrix()
    {
        return new Matrix(new double[][] { this.HorizontalAngle.ToArray(),
                                            this.VerticalAngle.ToArray()});
    }

    public IEllipsoidal Clone()
    {
        return new Ellipsoidal<TLinear, TAngular>(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                            "Copy of {0}", this.Name),
                                            this.HorizontalAngle.Clone(),
                                            this.VerticalAngle.Clone(),
                                            this.Datum,
                                            this.Handedness);
    }

    public Cartesian3D<T> ToCartesian<T>()
        where T : LinearUnit, new()
    {
        LinearCollection<T> x = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> y = new LinearCollection<T>(this.NumberOfPoints);

        LinearCollection<T> z = new LinearCollection<T>(this.NumberOfPoints);

        double tempSemiMinor = Datum.SemiMinorAxis.Value;

        double tempSemiMajor = Datum.SemiMajorAxis.Value;

        //double tempOriginX = this.AssociatedEllipsoid.Origin.X.Value;

        //double tempOriginY = this.AssociatedEllipsoid.Origin.Y.Value;

        //double tempOriginZ = this.AssociatedEllipsoid.Origin.Z.Value;

        //Matrix rotationMatrix = Transformation.CalculateEulerElementMatrix(AssociatedEllipsoid.Omega, AssociatedEllipsoid.Phi, AssociatedEllipsoid.Kappa);

        //Matrix transferMatrix = new Matrix(new double[][] { new double[] { tempOriginX, tempOriginY, tempOriginZ } });

        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            double tempN = this.Datum.CalculateN(this.VerticalAngle[i]).Value;

            double longCos = this.HorizontalAngle[i].Cos; double longSin = this.HorizontalAngle[i].Sin;

            double latCos = this.VerticalAngle[i].Cos; double latSin = this.VerticalAngle[i].Sin;

            Matrix tempGeodetic = new Matrix(new double[][]{new double[]{( tempN) * latCos * longCos,
                                                                            ( tempN) * latCos * longSin,
                                                                            ( tempN * tempSemiMinor * tempSemiMinor / (tempSemiMajor * tempSemiMajor)) * latSin}});

            //Matrix tempResult = rotationMatrix * tempGeodetic + transferMatrix;

            x.SetValue(i, new TLinear() { Value = tempGeodetic[0, 0] });

            y.SetValue(i, new TLinear() { Value = tempGeodetic[1, 0] });

            z.SetValue(i, new TLinear() { Value = tempGeodetic[2, 0] });
        }

        return new Cartesian3D<T>(x, y, z, this.Handedness);
    }

    public Ellipsoidal<TNewLinear, TNewAngular> ChangeTo<TNewLinear, TNewAngular>()
        where TNewLinear : LinearUnit, new()
        where TNewAngular : AngularUnit, new()
    {
        return new Ellipsoidal<TNewLinear, TNewAngular>(this.HorizontalAngle.ChangeTo<TNewAngular>(),
                                                    this.VerticalAngle.ChangeTo<TNewAngular>(),
                                                    this.Datum.ChangeTo<TNewLinear, TNewAngular>());
    }

    #endregion

    #region IEnumerable<IEllipsoidalPoint> Members

    public IEnumerator<IEllipsoidalPoint> GetEnumerator()
    {
        for (int i = 0; i < this.NumberOfPoints; i++)
        {
            yield return (EllipsoidalPoint<TLinear, TAngular>)this[i];
        }
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator(); ;
    }

    #endregion

}
