using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Sta.Common.Abstractions;

public interface IIdentifiable
{
    int Id { get; set; }

    Guid Key { get; set; }
}
