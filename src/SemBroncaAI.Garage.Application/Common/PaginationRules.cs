namespace SemBroncaAI.Garage.Application.Common;

public static class PaginationRules
{
    public const int MaximumPageSize = 100;
    public static void Validate(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "A página deve ser maior ou igual a 1.");
        ValidatePageSize(pageSize);
    }
    public static void ValidateOffset(int offset, int pageSize)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "O deslocamento não pode ser negativo.");
        ValidatePageSize(pageSize);
    }
    private static void ValidatePageSize(int pageSize)
    {
        if (pageSize is < 1 or > MaximumPageSize)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"O tamanho da página deve estar entre 1 e {MaximumPageSize}.");
    }
}
