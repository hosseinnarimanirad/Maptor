using IRI.Maptor.Presentation.Wpf.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Controls;

public class MapMarker : NotifiableUserControl, IMapMarker
{
    public virtual bool IsSelected { get; set; }
}
