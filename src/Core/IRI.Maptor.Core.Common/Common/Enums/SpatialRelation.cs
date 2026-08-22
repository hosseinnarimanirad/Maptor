using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Enums;

public enum SpatialRelation
{ 
    Disjoint,       //The geometries have no points in common
    Intersects,     //The geometries have at least one point in common
    //Touches,        //The geometries only touch edges and do not overlap in any way
    //Crosses,        //The geometries do more than touch, they actually overlap edges
    //Within,         //One geometry is completely within another (no touching edges)
    Contained,      //One geometry is completely within another (touching edges)
    Contains,       //One geometry contains another
    Overlaps,       //The geometries have some points in common; but not all points in common 
                    //  (so if one geometry is inside the other overlaps would be false). 
                    //  The overlapping section must be the same kind of shape as the two geometries; 
                    //  so two polygons that touch on a point are not considered to be overlapping
}
