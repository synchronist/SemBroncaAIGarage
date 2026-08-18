using SemBroncaAI.Garage.Application.Abstractions.Security;

namespace SemBroncaAI.Garage.Web.Services;

public static class LoginErrorMessages
{
    public static string Resolve(string? error) => error switch
    {
        "limited" => "Muitas tentativas. Aguarde um minuto e tente novamente.",
        "locked" => "Acesso temporariamente bloqueado por excesso de tentativas. Tente novamente mais tarde.",
        AuthenticationErrorCodes.GarageInactive => "O acesso desta oficina está temporariamente indisponível.",
        "unavailable" => "O acesso está temporariamente indisponível. Tente novamente.",
        _ => "Não foi possível entrar com as credenciais informadas."
    };
}
