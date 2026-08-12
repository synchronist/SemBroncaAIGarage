using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class ApprovalTokenService(IDataProtectionProvider provider) : IApprovalTokenService
{
    private readonly IDataProtector _protector = provider.CreateProtector("SBGarage.EstimateApprovalToken.v1");
    public ApprovalToken Create()
    {
        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return new(value, Hash(value), _protector.Protect(value));
    }
    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public string Unprotect(string protectedToken) => _protector.Unprotect(protectedToken);
}
