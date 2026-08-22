using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Mathematics;

public class ClassFrequency<T>
{
    public T @Class { get; set; }

    public int Count { get; set; }

    public ClassFrequency(T @class, int count)
    {
        this.Class = @class;

        this.Count = count;
    }

    public override string ToString()
    {
        return $"{Class} ({Count})";
    }
}
