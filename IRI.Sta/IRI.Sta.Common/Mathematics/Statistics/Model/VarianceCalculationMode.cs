﻿using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Sta.Mathematics;

public enum VarianceCalculationMode
{
    /// <summary>
    /// divide by n-1
    /// </summary>
    Sample = 1,

    /// <summary>
    /// divide by n
    /// </summary>
    Population = 2
}
