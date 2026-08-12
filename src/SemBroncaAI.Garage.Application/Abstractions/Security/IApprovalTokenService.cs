namespace SemBroncaAI.Garage.Application.Abstractions.Security;

public interface IApprovalTokenService
{
    ApprovalToken Create();
    string Hash(string token);
    string Unprotect(string protectedToken);
}

public sealed record ApprovalToken(string Value, string Hash, string ProtectedValue);
