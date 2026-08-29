namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public static class ServiceOrderMessages
{
    public const string Created =
        "Ordem de serviço criada e veículo recebido.";

    public const string DiagnosisStarted =
        "Diagnóstico iniciado.";

    public const string SentForApproval =
        "Orçamento enviado para aprovação do cliente.";

    public const string DigitalApprovalWaived =
        "Aceite digital dispensado pela oficina.";

    public const string ServiceStarted =
        "Serviço iniciado.";

    public const string PartiallyApprovedServiceStarted =
        "Serviço iniciado somente para os itens aprovados pelo cliente.";

    public const string WaitingParts =
        "Ordem aguardando peças.";

    public const string ServiceResumed =
        "Serviço retomado.";

    public const string ServiceFinished =
        "Serviço concluído.";

    public const string VehicleDelivered =
        "Veículo entregue ao cliente.";

    public const string Cancelled =
        "Ordem de serviço cancelada.";
}
