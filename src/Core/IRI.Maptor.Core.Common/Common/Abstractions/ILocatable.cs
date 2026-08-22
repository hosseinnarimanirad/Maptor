using IRI.Maptor.Core.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.Common.Abstractions;

public interface ILocatable
{
    Point Location { get; set; }
}
