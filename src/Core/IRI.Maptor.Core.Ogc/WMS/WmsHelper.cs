using IRI.Maptor.Core.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 

namespace IRI.Maptor.Core.Ogc.WMS;

public static class WmsHelper
{
    public static BoundingBox ParseCrs(string version, string crs, string bbx)
    {
        try
        {
            var items = bbx.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToList();

            if (items.Count != 4)
            {
                throw new NotImplementedException("Invalid input in BoundingBox.Parse");
            }

            if (version.Trim() == WmsConstants.version130)
            {
                if (crs.Trim() == WmsConstants.Epsg4326)
                {
                    return new BoundingBox(xMin: items[1], yMin: items[0], xMax: items[3], yMax: items[2]);
                }
                else //if (crs.Trim() == WmsConstants.Crs84)
                {
                    return new BoundingBox(xMin: items[0], yMin: items[1], xMax: items[2], yMax: items[3]);
                }
            }
            else if (version.Trim() == WmsConstants.version111)
            {
                if (crs.Trim() == WmsConstants.Epsg4326)
                {
                    return new BoundingBox(xMin: items[0], yMin: items[1], xMax: items[2], yMax: items[3]);
                }
            }

            throw new NotImplementedException();

        }
        catch (Exception)
        {
            throw new NotImplementedException("ERROR AT BoundingBox.Parse");
        }
    }
}
