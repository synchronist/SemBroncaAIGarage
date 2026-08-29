using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.SiteManagement;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Api;
public sealed class PlatformSitesTests
{
 [Fact]public void Site_manager_api_must_remain_inside_platform_admin_boundary(){var authorization=typeof(PlatformSitesController).GetCustomAttribute<AuthorizeAttribute>();authorization.ShouldNotBeNull();authorization.Policy.ShouldBe(PlatformAuthorization.Policy);typeof(PlatformSitesController).GetCustomAttribute<AllowAnonymousAttribute>().ShouldBeNull();}
 [Fact]public void Contracts_must_not_expose_password_or_secret_values(){var names=new[]{typeof(ManagedSiteSaveCommand),typeof(ManagedSiteMailboxInput),typeof(ManagedSiteCostInput)}.SelectMany(x=>x.GetProperties()).Select(x=>x.Name).ToArray();names.ShouldNotContain(x=>x.Contains("Password",StringComparison.OrdinalIgnoreCase));names.ShouldNotContain(x=>x.Contains("Secret",StringComparison.OrdinalIgnoreCase));names.ShouldContain("CredentialReference");}
 [Fact]public void Platform_pages_should_preserve_authorization_responsive_list_and_snackbar_feedback(){var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var list=File.ReadAllText(Path.Combine(root,"src","SemBroncaAI.Garage.Web","Components","Pages","PlatformSites.razor"));var edit=File.ReadAllText(Path.Combine(root,"src","SemBroncaAI.Garage.Web","Components","Pages","PlatformSiteEdit.razor"));var css=File.ReadAllText(Path.Combine(root,"src","SemBroncaAI.Garage.Web","Components","Pages","PlatformSites.razor.css"));list.ShouldContain("Authorize(Policy=PlatformAuthorization.Policy)");edit.ShouldContain("Authorize(Policy=PlatformAuthorization.Policy)");list.ShouldContain("ISnackbar");edit.ShouldContain("ISnackbar");list.ShouldContain("mobile-list");css.ShouldContain("@media(max-width:720px)");}
}
