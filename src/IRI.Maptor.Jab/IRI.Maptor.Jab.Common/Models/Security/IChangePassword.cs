using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models.Security;

public interface IChangePassword : INewPassword, IHavePassword
{ 
    //SecureString Password { get; }
     
    //void ClearInputValues();
}
