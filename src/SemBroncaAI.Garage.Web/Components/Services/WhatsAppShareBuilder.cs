using System.Globalization;
using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public sealed class WhatsAppShareBuilder(IConfiguration configuration)
{
    public WhatsAppShareModel Build(EstimateListItemModel estimate, string garageName, string? garagePhone,
        string currentWebBaseUrl)
    {
        var link = BuildApprovalLink(
            estimate.ApprovalToken
                ?? throw new InvalidOperationException("Este orçamento não possui um link de aprovação disponível."),
            currentWebBaseUrl);
        var firstName = estimate.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "cliente";
        var phone = NormalizeBrazilianPhone(estimate.CustomerPhone);
        var contact = string.IsNullOrWhiteSpace(garagePhone) ? string.Empty : $"\nContato da oficina: {garagePhone.Trim()}\n";
        var message = $"Olá, {firstName}!\n\n{garageName} preparou o orçamento do seu {estimate.Vehicle} — placa {estimate.Plate}.\n\n" +
            $"OS #{estimate.ServiceOrderNumber:D4}\nValor: {estimate.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}\n\n" +
            $"Confira os detalhes e aprove ou recuse pelo link:\n{link}\n{contact}\nSe tiver alguma dúvida, estamos à disposição.\n\n{garageName}";
        return new(estimate.CustomerName, estimate.CustomerPhone, message, link, phone);
    }

    public string BuildApprovalLink(string token, string currentWebBaseUrl)
    {
        var configured = configuration["PublicAppBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configured) ? currentWebBaseUrl : configured;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
            (!baseUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !baseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A URL pública da aplicação não foi configurada corretamente.");
        return new Uri(baseUri, $"approval/{Uri.EscapeDataString(token)}").AbsoluteUri;
    }

    public static string? NormalizeBrazilianPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00")) digits = digits[2..];
        if (digits.StartsWith("55") && digits.Length is 12 or 13) return digits;
        if (digits.Length is 10 or 11) return $"55{digits}";
        return null;
    }
}
