using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IRI.Maptor.Sta.Ogc;

 

[XmlRoot("Filter_Capabilities", Namespace = SldNamespaces.OGC)]
public class FilterCapabilities
{
    [XmlElement("Spatial_Capabilities")]
    public SpatialCapabilitiesType SpatialCapabilities { get; set; }

    [XmlElement("Scalar_Capabilities")]
    public ScalarCapabilitiesType ScalarCapabilities { get; set; }

    [XmlElement("Id_Capabilities")]
    public IdCapabilitiesType IdCapabilities { get; set; }
}

public class SpatialCapabilitiesType
{
    [XmlElement("GeometryOperands")]
    public GeometryOperandsType GeometryOperands { get; set; }

    [XmlElement("SpatialOperators")]
    public SpatialOperatorsType SpatialOperators { get; set; }
}

public class GeometryOperandsType
{
    [XmlElement("GeometryOperand")]
    public List<GeometryOperandType> GeometryOperands { get; set; } = new();
}

public class SpatialOperatorsType
{
    [XmlElement("SpatialOperator")]
    public List<SpatialOperatorType> SpatialOperators { get; set; } = new();
}

public class SpatialOperatorType
{
    [XmlElement("GeometryOperands")]
    public GeometryOperandsType GeometryOperands { get; set; }

    [XmlAttribute("name")]
    public SpatialOperatorNameType Name { get; set; }
}

public class ScalarCapabilitiesType
{
    [XmlElement("LogicalOperators")]
    public LogicalOperators LogicalOperators { get; set; }

    [XmlElement("ComparisonOperators")]
    public ComparisonOperatorsType ComparisonOperators { get; set; }

    [XmlElement("ArithmeticOperators")]
    public ArithmeticOperatorsType ArithmeticOperators { get; set; }
}

public class ComparisonOperatorsType
{
    [XmlElement("ComparisonOperator")]
    public List<ComparisonOperatorType> ComparisonOperators { get; set; } = new();
}

public class ArithmeticOperatorsType
{
    [XmlElement("SimpleArithmetic")]
    public SimpleArithmetic SimpleArithmetic { get; set; }

    [XmlElement("Functions")]
    public FunctionsType Functions { get; set; }
}

public class FunctionsType
{
    [XmlElement("FunctionNames")]
    public FunctionNamesType FunctionNames { get; set; }
}

public class FunctionNamesType
{
    [XmlElement("FunctionName")]
    public List<FunctionNameType> FunctionNames { get; set; } = new();
}

public class FunctionNameType
{
    [XmlText]
    public string Value { get; set; }

    [XmlAttribute("nArgs")]
    public string NArgs { get; set; }
}

public class IdCapabilitiesType
{
    [XmlElement("EID")]
    public List<EID> EIDs { get; set; } = new();

    [XmlElement("FID")]
    public List<FID> FIDs { get; set; } = new();
}

public class LogicalOperators { }
public class SimpleArithmetic { }
public class EID { }
public class FID { }

 