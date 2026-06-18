using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Core.Models.Security;

public interface INewUserEmailPassword : INewPassword
{
    string UserNameOrEmail { get; set; }

    bool IsValidEmail();
}
