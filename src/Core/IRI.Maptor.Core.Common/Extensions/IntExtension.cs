using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Extensions;

public static class IntExtension
{
    public static string Number2String(this int number, bool isCaps)
    {
        char c = (char)((isCaps ? 65 : 97) + number);

        return c.ToString();
    }
}
