using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Web.Models;

public static class SubscriptionPresentation
{
    public static string PlanLabel(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Standard => "Standard",
        _ => "Plano atual"
    };

    public static string StatusLabel(SubscriptionStatus status, DateTime? trialEndsAt, DateTime now) => status switch
    {
        SubscriptionStatus.Trial when IsTrialExpired(trialEndsAt, now) => "Período gratuito encerrado",
        SubscriptionStatus.Trial => "Período gratuito",
        SubscriptionStatus.Active => "Assinatura ativa",
        SubscriptionStatus.PastDue => "Aguardando regularização",
        SubscriptionStatus.Suspended => "Assinatura suspensa",
        SubscriptionStatus.Cancelled => "Assinatura cancelada",
        _ => "Status indisponível"
    };

    public static bool IsTrialExpired(DateTime? trialEndsAt, DateTime now) =>
        trialEndsAt.HasValue && trialEndsAt.Value < now;

    public static int RemainingTrialDays(DateTime? trialEndsAt, DateTime now) =>
        !trialEndsAt.HasValue || IsTrialExpired(trialEndsAt, now)
            ? 0
            : (int)Math.Ceiling((trialEndsAt.Value - now).TotalDays);

    public static string StatusDescription(SubscriptionStatus status, DateTime? trialEndsAt, DateTime now) => status switch
    {
        SubscriptionStatus.Trial when IsTrialExpired(trialEndsAt, now) && trialEndsAt.HasValue =>
            $"Seu período gratuito terminou em {trialEndsAt.Value:dd/MM/yyyy}.",
        SubscriptionStatus.Trial => TrialRemainingDescription(RemainingTrialDays(trialEndsAt, now)),
        SubscriptionStatus.Active => "Seu plano está ativo e disponível para a oficina.",
        SubscriptionStatus.PastDue => "A assinatura aguarda regularização. Consulte o suporte para mais informações.",
        SubscriptionStatus.Suspended => "A assinatura está suspensa. As regras operacionais atuais da oficina continuam válidas.",
        SubscriptionStatus.Cancelled => "A assinatura foi cancelada. Consulte o suporte para conhecer as opções disponíveis.",
        _ => "Não foi possível identificar a situação da assinatura."
    };

    private static string TrialRemainingDescription(int days) => days switch
    {
        0 => "Seu período gratuito termina hoje.",
        1 => "Resta 1 dia do seu período gratuito.",
        _ => $"Restam {days} dias do seu período gratuito."
    };
}
