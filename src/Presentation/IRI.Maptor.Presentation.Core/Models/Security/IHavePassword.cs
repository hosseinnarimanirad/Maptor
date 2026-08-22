using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Presentation.Core.Models.Security;

public interface IHavePassword : ISecurityBase
{
    System.Security.SecureString Password { get; }

    string GetPasswordText();
}
