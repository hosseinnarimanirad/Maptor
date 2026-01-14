using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models;

public class IdentifyOptions
{
    // ignore unvisible layers or not in identify
    public bool CheckIsVisible { get; set; } = true;

    // ignore layers which are not in scale range or not
    public bool CheckIsInScaleRange { get; set; } = true;
}
