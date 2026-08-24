using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class ReceiveVehicleActionTests
{
    private readonly string _markup = File.ReadAllText(FindRepositoryFile(
        "src", "SemBroncaAI.Garage.Web", "Components", "Pages", "Home.razor"));

    [Fact]
    public void Should_not_expose_unimplemented_voice_or_placeholder_actions()
    {
        _markup.ShouldNotContain("Ditar relato");
        _markup.ShouldNotContain("Digite ou dite");
        _markup.ShouldNotContain("Salvar rascunho");
        _markup.ShouldNotContain("Receber e imprimir ficha");
    }

    [Fact]
    public void Should_preserve_manual_complaint_and_working_receive_action()
    {
        _markup.ShouldContain("@bind-Value=\"_customerComplaint\"");
        _markup.ShouldContain("Placeholder=\"Digite o que precisa ser verificado...\"");
        _markup.ShouldContain("OnClick=\"ReceiveVehicleAsync\"");
        _markup.ShouldContain("<span>Receber veículo</span>");
    }

    private static string FindRepositoryFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (!File.Exists(Path.Combine(directory.FullName, "SemBroncaAI.Garage.slnx"))) continue;
            return Path.Combine([directory.FullName, .. parts]);
        }

        throw new DirectoryNotFoundException("A raiz do repositório não foi encontrada.");
    }
}
