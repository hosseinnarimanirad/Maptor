using IRI.Maptor.Sta.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.Common.Abstractions;

public interface ILocatable
{
    Point Location { get; set; }
}
