using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Infrastructure.PersonalGdb.Xml;


// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlType(AnonymousType = true)]
[System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
public partial class DEFeatureDataset
{

    private string catalogPathField;

    private string nameField;

    private bool childrenExpandedField;

    private string datasetTypeField;

    private byte dSIDField;

    private bool versionedField;

    private bool canVersionField;

    private object configurationKeywordField;

    private decimal requiredGeodatabaseClientVersionField;

    private object extentField;

    private DEFeatureDatasetSpatialReference spatialReferenceField;

    private bool changeTrackedField;

    /// <remarks/>
    public string CatalogPath
    {
        get
        {
            return catalogPathField;
        }
        set
        {
            catalogPathField = value;
        }
    }

    /// <remarks/>
    public string Name
    {
        get
        {
            return nameField;
        }
        set
        {
            nameField = value;
        }
    }

    /// <remarks/>
    public bool ChildrenExpanded
    {
        get
        {
            return childrenExpandedField;
        }
        set
        {
            childrenExpandedField = value;
        }
    }

    /// <remarks/>
    public string DatasetType
    {
        get
        {
            return datasetTypeField;
        }
        set
        {
            datasetTypeField = value;
        }
    }

    /// <remarks/>
    public byte DSID
    {
        get
        {
            return dSIDField;
        }
        set
        {
            dSIDField = value;
        }
    }

    /// <remarks/>
    public bool Versioned
    {
        get
        {
            return versionedField;
        }
        set
        {
            versionedField = value;
        }
    }

    /// <remarks/>
    public bool CanVersion
    {
        get
        {
            return canVersionField;
        }
        set
        {
            canVersionField = value;
        }
    }

    /// <remarks/>
    public object ConfigurationKeyword
    {
        get
        {
            return configurationKeywordField;
        }
        set
        {
            configurationKeywordField = value;
        }
    }

    /// <remarks/>
    public decimal RequiredGeodatabaseClientVersion
    {
        get
        {
            return requiredGeodatabaseClientVersionField;
        }
        set
        {
            requiredGeodatabaseClientVersionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElement(IsNullable = true)]
    public object Extent
    {
        get
        {
            return extentField;
        }
        set
        {
            extentField = value;
        }
    }

    /// <remarks/>
    public DEFeatureDatasetSpatialReference SpatialReference
    {
        get
        {
            return spatialReferenceField;
        }
        set
        {
            spatialReferenceField = value;
        }
    }

    /// <remarks/>
    public bool ChangeTracked
    {
        get
        {
            return changeTrackedField;
        }
        set
        {
            changeTrackedField = value;
        }
    }
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlType(AnonymousType = true)]
public partial class DEFeatureDatasetSpatialReference
{

    private string wKTField;

    private short xOriginField;

    private short yOriginField;

    private decimal xYScaleField;

    private int zOriginField;

    private ushort zScaleField;

    private int mOriginField;

    private ushort mScaleField;

    private double xYToleranceField;

    private decimal zToleranceField;

    private decimal mToleranceField;

    private bool highPrecisionField;

    private short leftLongitudeField;

    private ushort wKIDField;

    private ushort latestWKIDField;

    /// <remarks/>
    public string WKT
    {
        get
        {
            return wKTField;
        }
        set
        {
            wKTField = value;
        }
    }

    /// <remarks/>
    public short XOrigin
    {
        get
        {
            return xOriginField;
        }
        set
        {
            xOriginField = value;
        }
    }

    /// <remarks/>
    public short YOrigin
    {
        get
        {
            return yOriginField;
        }
        set
        {
            yOriginField = value;
        }
    }

    /// <remarks/>
    public decimal XYScale
    {
        get
        {
            return xYScaleField;
        }
        set
        {
            xYScaleField = value;
        }
    }

    /// <remarks/>
    public int ZOrigin
    {
        get
        {
            return zOriginField;
        }
        set
        {
            zOriginField = value;
        }
    }

    /// <remarks/>
    public ushort ZScale
    {
        get
        {
            return zScaleField;
        }
        set
        {
            zScaleField = value;
        }
    }

    /// <remarks/>
    public int MOrigin
    {
        get
        {
            return mOriginField;
        }
        set
        {
            mOriginField = value;
        }
    }

    /// <remarks/>
    public ushort MScale
    {
        get
        {
            return mScaleField;
        }
        set
        {
            mScaleField = value;
        }
    }

    /// <remarks/>
    public double XYTolerance
    {
        get
        {
            return xYToleranceField;
        }
        set
        {
            xYToleranceField = value;
        }
    }

    /// <remarks/>
    public decimal ZTolerance
    {
        get
        {
            return zToleranceField;
        }
        set
        {
            zToleranceField = value;
        }
    }

    /// <remarks/>
    public decimal MTolerance
    {
        get
        {
            return mToleranceField;
        }
        set
        {
            mToleranceField = value;
        }
    }

    /// <remarks/>
    public bool HighPrecision
    {
        get
        {
            return highPrecisionField;
        }
        set
        {
            highPrecisionField = value;
        }
    }

    /// <remarks/>
    public short LeftLongitude
    {
        get
        {
            return leftLongitudeField;
        }
        set
        {
            leftLongitudeField = value;
        }
    }

    /// <remarks/>
    public ushort WKID
    {
        get
        {
            return wKIDField;
        }
        set
        {
            wKIDField = value;
        }
    }

    /// <remarks/>
    public ushort LatestWKID
    {
        get
        {
            return latestWKIDField;
        }
        set
        {
            latestWKIDField = value;
        }
    }
}

