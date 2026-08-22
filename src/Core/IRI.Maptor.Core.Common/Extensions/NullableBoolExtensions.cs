using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class NullableBoolExtensions
{
    public static bool IsTrue(this bool? value)
    {
        return value.HasValue && value.Value == true;
    }
}
