using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models.Legend;

public class LegendItemEventArgs : EventArgs
{
    public LegendItem Item { get; private set; }

    public LegendItemEventArgs(LegendItem item)
    {
        this.Item = item;
    }
}
