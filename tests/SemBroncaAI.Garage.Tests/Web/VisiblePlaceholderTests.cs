using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class VisiblePlaceholderTests
{
    [Fact]
    public void Operational_layout_should_not_expose_a_dead_notifications_button()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var layout = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web", "Components", "Layout", "MainLayout.razor"));

        layout.ShouldNotContain("Notificações");
        layout.ShouldNotContain("NotificationsNone");
    }

    [Fact]
    public void Operational_layout_should_expose_future_ai_and_whatsapp_as_disabled()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var layout = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web", "Components", "Layout", "MainLayout.razor"));

        layout.ShouldContain("IA &amp; WhatsApp");
        layout.ShouldContain("Em construção");
        layout.ShouldContain("sbg-menu-item-disabled");
        layout.ShouldContain("aria-disabled=\"true\"");
        layout.ShouldNotContain("href=\"/ai");
        layout.ShouldNotContain("href=\"/whatsapp");
    }

    [Fact]
    public void Global_error_page_should_not_expose_development_instructions()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web", "Components", "Pages", "Error.razor"));

        page.ShouldContain("Não foi possível concluir esta ação.");
        page.ShouldContain("Código de atendimento");
        page.ShouldNotContain("Development Mode");
        page.ShouldNotContain("ASPNETCORE_ENVIRONMENT");
    }
}
