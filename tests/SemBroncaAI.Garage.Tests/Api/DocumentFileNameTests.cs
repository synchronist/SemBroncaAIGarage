using SemBroncaAI.Garage.Api.Services;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class DocumentFileNameTests
{
    [Theory]
    [InlineData("OS", 12, "ABC-1D23", "OS-0012-ABC1D23.pdf")]
    [InlineData("ORÇAMENTO", 7, "abc 1234", "ORCAMENTO-0007-ABC1234.pdf")]
    public void Should_Create_Safe_File_Name(string prefix, int number, string plate, string expected) =>
        DocumentFileName.Create(prefix, number, plate).ShouldBe(expected);
}
