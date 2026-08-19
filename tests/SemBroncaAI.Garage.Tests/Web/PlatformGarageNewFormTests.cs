using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class PlatformGarageNewFormTests
{
    private readonly string _markup = File.ReadAllText(FindRepositoryFile(
        "src", "SemBroncaAI.Garage.Web", "Components", "Pages", "PlatformGarageNew.razor"));
    private readonly string _javascript = File.ReadAllText(FindRepositoryFile(
        "src", "SemBroncaAI.Garage.Web", "wwwroot", "js", "auth.js"));

    [Fact]
    public void Enter_is_ignored_safely_without_replacing_the_form_model()
    {
        _markup.ShouldContain("data-prevent-implicit-submit");
        _javascript.ShouldContain("event.target.form?.hasAttribute(\"data-prevent-implicit-submit\")");
        _javascript.ShouldContain("}, true);");
        _markup.ShouldContain("private readonly FormModel _form = new();");
        Count("private readonly FormModel _form = new();").ShouldBe(1);
    }

    [Fact]
    public void Invalid_submit_preserves_typed_values()
    {
        _markup.ShouldContain("OnSubmit=\"HandleSubmitAsync\"");
        _markup.ShouldContain("sbgReadPlatformGarageForm");
        _markup.ShouldContain("private void HandleInvalidSubmit(EditContext _) { _saving = false; _error = \"Revise os campos destacados abaixo.\"; }");
    }

    [Fact]
    public void Password_visibility_buttons_never_submit_the_form()
    {
        Count("<button type=\"button\" @onclick=\"TogglePassword\"").ShouldBe(2);
    }

    [Fact]
    public void Backend_errors_preserve_the_form_for_resubmission()
    {
        _markup.ShouldContain("catch (PlatformGarageFormValidationException exception)");
        _markup.ShouldContain("catch { _error = \"Não foi possível concluir o cadastro. Tente novamente.\"; }");
        Count("_form =").ShouldBe(1);
    }

    [Fact]
    public void Generic_api_error_is_not_treated_as_field_validation()
    {
        var service = File.ReadAllText(FindRepositoryFile(
            "src", "SemBroncaAI.Garage.Web", "Components", "Services", "PlatformGarageService.cs"));

        service.ShouldContain("validation?.Errors is { Count: > 0 } ? validation : null");
    }

    [Fact]
    public void Create_button_remains_the_explicit_valid_submit_path()
    {
        _markup.ShouldContain("OnSubmit=\"HandleSubmitAsync\"");
        _markup.ShouldContain("await SubmitAsync();");
        _markup.ShouldContain("<button type=\"submit\" disabled=\"@_saving\"");
        _markup.ShouldContain("Service.CreateAsync(");
    }

    private int Count(string value) =>
        _markup.Split(value, StringSplitOptions.None).Length - 1;

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
