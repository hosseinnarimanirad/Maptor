using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models.Security;

public interface IUserEmailPassword : IHavePassword
{
    string UserNameOrEmail { get; set; }

    bool IsValidEmail();
}
