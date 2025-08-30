using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models;

public interface IProxySettings
{
    bool IsProxyMode { get; set; }
    string Address { get; set; }
    int Port { get; set; }
    string UserId { get; set; }
    string UserPass { get; set; }
}
