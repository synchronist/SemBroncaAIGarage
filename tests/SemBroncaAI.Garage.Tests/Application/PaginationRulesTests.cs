using SemBroncaAI.Garage.Application.Common;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application;

public sealed class PaginationRulesTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public void Invalid_page_parameters_should_be_rejected(int page, int pageSize) =>
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationRules.Validate(page, pageSize));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    public void Valid_page_parameters_should_be_accepted(int page, int pageSize) =>
        Should.NotThrow(() => PaginationRules.Validate(page, pageSize));

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public void Invalid_offset_parameters_should_be_rejected(int offset, int pageSize) =>
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationRules.ValidateOffset(offset, pageSize));

    [Fact]
    public void Technical_history_batches_should_accept_initial_two_and_subsequent_five()
    {
        Should.NotThrow(() => PaginationRules.ValidateOffset(0, 2));
        Should.NotThrow(() => PaginationRules.ValidateOffset(2, 5));
    }
}
