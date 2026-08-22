// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using IRI.Maptor.Core.Common.Metrics;

namespace IRI.Maptor.Core.SpatialReferenceSystem.Models;

public struct OrientationParameter
{
    private AngularUnit m_Omega, m_Phi, m_Kappa;

    public AngularUnit Omega
    {
        get { return m_Omega; }
    }

    public AngularUnit Phi
    {
        get { return m_Omega; }
    }

    public AngularUnit Kappa
    {
        get { return m_Omega; }
    }

    public OrientationParameter(AngularUnit omega, AngularUnit phi, AngularUnit kappa)
    {
        m_Omega = omega;

        m_Phi = phi;

        m_Kappa = kappa;
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() == typeof(OrientationParameter))
        {
            return this == (OrientationParameter)obj;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "Omega: {0}, Phi: {1}, Kappa: {2}",
                                Omega,
                                Phi,
                                Kappa);
    }

    public static bool operator ==(OrientationParameter firstValue, OrientationParameter secondValue)
    {
        return firstValue.Omega == secondValue.Omega &&
                firstValue.Phi == secondValue.Phi &&
                firstValue.Kappa == secondValue.Kappa;
    }

    public static bool operator !=(OrientationParameter firstValue, OrientationParameter secondValue)
    {
        return !(firstValue == secondValue);
    }
}
