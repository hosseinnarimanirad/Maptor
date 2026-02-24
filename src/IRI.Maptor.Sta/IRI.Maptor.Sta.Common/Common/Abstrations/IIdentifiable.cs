using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Abstrations;

public interface IIdentifiable
{
    int Id { get; set; }

    Guid Key { get; set; }
}
