namespace SemBroncaAI.Garage.Application.Features.PlatformAdministration;

public sealed class PlatformGarageValidationException(
    IReadOnlyDictionary<string, string[]> errors)
    : Exception("Revise os campos informados.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class PlatformGarageConflictException(string field, string message)
    : Exception(message)
{
    public string Field { get; } = field;
}
