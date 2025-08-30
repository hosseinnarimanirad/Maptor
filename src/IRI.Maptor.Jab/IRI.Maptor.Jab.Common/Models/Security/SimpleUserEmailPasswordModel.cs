using System.Security;

using IRI.Maptor.Sta.Common.Helpers;

namespace IRI.Maptor.Jab.Common.Models.Security;

public class SimpleUserEmailPasswordModel : IUserEmailPassword
{
    //public string UserName { get; set; }

    //public string Password { get; set; }

    public SimpleUserEmailPasswordModel(SecureString password)
    {
        this.Password = password;
    }

    public string UserNameOrEmail { get; set; }

    public SecureString Password { get; private set; }

    public void ClearInputValues()
    {
        Password.Clear();

        UserNameOrEmail = string.Empty;
    }

    public string GetPasswordText()
    {
        return SecureStringHelper.GetString(Password);
    }

    public bool IsValidEmail()
    {
        return NetHelper.IsValidEmail(UserNameOrEmail);
    }
}
