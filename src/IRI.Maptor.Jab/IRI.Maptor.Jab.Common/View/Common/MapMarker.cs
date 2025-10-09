using IRI.Maptor.Jab.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IRI.Maptor.Jab.Common.View;

public class MapMarker : NotifiableUserControl, IMapMarker
{
    public virtual bool IsSelected { get; set; }
}
