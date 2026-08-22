// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Globalization;

namespace IRI.Maptor.Core.Common.Metrics;

public static class AngularUnitBuilder
{

    static AngularUnitBuilder()
    {
        m_UnitPairs = new Dictionary<AngleMode, Type>();

        m_UnitPairs.Add(AngleMode.Degree, typeof(Degree));

        m_UnitPairs.Add(AngleMode.Grade, typeof(Grade));

        m_UnitPairs.Add(AngleMode.Radian, typeof(Radian));
    }

    public static AngularUnit Build(double value, AngleMode mode, AngleRange range)
    {

        switch (mode)
        {
            case AngleMode.Degree:

                return new Degree(value, range);

            case AngleMode.Grade:

                return new Grade(value, range);

            case AngleMode.Radian:

                return new Radian(value, range);

            default:
                throw new NotImplementedException();

        }

    }

    public static AngularUnit BuildFromRadianValue(double value, AngleMode mode, AngleRange range)
    {

        switch (mode)
        {
            case AngleMode.Degree:

                value = UnitConversion.RadianToDegree(value);

                break;

            case AngleMode.Grade:

                value = UnitConversion.RadianToGrade(value);

                break;

            case AngleMode.Radian:

                break;

            default:
                throw new NotImplementedException();
        }

        return Build(value, mode, range);

    }

    private static Dictionary<AngleMode, System.Type> m_UnitPairs;

    public static Dictionary<AngleMode, System.Type> UnitPairs
    {
        get { return AngularUnitBuilder.m_UnitPairs; }
    }
}
