using System.Security;

namespace IRI.Maptor.Presentation.Core.Models.Security;

public interface INewPassword : ISecurityBase
{
    SecureString NewPassword { get; }

    SecureString ConfirmPassword { get; }

    bool IsNewPasswordValid();

    string GetNewPasswordText();
}
