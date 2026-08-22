// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Metrics;

public static class LinearUnitBuilder
{
    static LinearUnitBuilder()
    {
        m_UnitPairs = new Dictionary<LinearMode, Type>();
        
        m_UnitPairs.Add(LinearMode.Meter, typeof(Meter));

        m_UnitPairs.Add(LinearMode.Mile, typeof(Mile));

        m_UnitPairs.Add(LinearMode.Yard, typeof(Yard));

        m_UnitPairs.Add(LinearMode.Foot, typeof(Foot));

        m_UnitPairs.Add(LinearMode.Inch, typeof(Inch));

        m_UnitPairs.Add(LinearMode.Rod, typeof(Rod));

        m_UnitPairs.Add(LinearMode.Chain, typeof(Chain));
    }

    public static LinearUnit Build(double value, LinearMode mode)
    {

        switch (mode)
        {
            case LinearMode.Meter:
                return new Meter(value);//, LinearPrefix.Nothing);

            case LinearMode.Mile:
                return new Mile(value);

            case LinearMode.Yard:
                return new Yard(value);

            case LinearMode.Foot:
                return new Foot(value);

            case LinearMode.Inch:
                return new Inch(value);

            case LinearMode.Rod:
                return new Rod(value);

            case LinearMode.Chain:
                return new Chain(value);

            default:
                throw new NotImplementedException();
        }
    }

    private static Dictionary<LinearMode, System.Type> m_UnitPairs;

    public static Dictionary<LinearMode, System.Type> UnitPairs
    {
        get { return LinearUnitBuilder.m_UnitPairs; }
    }
}
