using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Extensions;

public static class NullableIntExtensions
{
    public static int GetValue(this int? value)
    {
        return value == null ? 0 : value.HasValue ? value.Value : 0;
    }
}
