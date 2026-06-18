using System.Security;

namespace IRI.Maptor.Jab.Core.Models.Security;

public interface INewPassword : ISecurityBase
{
    SecureString NewPassword { get; }

    SecureString ConfirmPassword { get; }

    bool IsNewPasswordValid();

    string GetNewPasswordText();
}
