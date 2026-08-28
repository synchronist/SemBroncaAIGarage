using Shouldly;

namespace SemBroncaAI.Garage.Tests.Web;

public sealed class ServiceOrderApprovalActionsTests
{
    [Fact]
    public void Approval_Actions_Should_Share_The_Same_Public_Url_And_Provide_Browser_Fallbacks()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web",
            "Components", "Pages", "ServiceOrderDetails.razor"));
        var script = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web",
            "wwwroot", "js", "downloads.js"));

        page.ShouldContain("InvokeAsync<bool>(\"sbgCopyText\", ApprovalLink)");
        page.ShouldContain("InvokeVoidAsync(\"sbgOpenUrl\", ApprovalLink)");
        script.ShouldContain("navigator.clipboard?.writeText");
        script.ShouldContain("document.execCommand(\"copy\")");
        script.ShouldContain("window.location.assign(url)");
    }

    [Fact]
    public void Pdf_Only_Action_Should_Open_Existing_Estimate_Print_Flow_And_Explicitly_Waive_Digital_Approval()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "src", "SemBroncaAI.Garage.Web",
            "Components", "Pages", "ServiceOrderDetails.razor"));

        page.ShouldContain("Enviar apenas PDF");
        page.ShouldContain("_serviceOrder.Status == \"Diagnosis\" || _serviceOrder.Status == \"WaitingApproval\"");
        page.ShouldContain("approval-mobile-actions");
        page.ShouldContain("EstimatePrintUrl");
        page.ShouldContain("Navigation.NavigateTo(EstimatePrintUrl)");
        page.ShouldContain("ExecuteTransitionAsync(Id, \"waive-digital-approval\")");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "SemBroncaAI.Garage.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
