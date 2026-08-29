using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Application.Features.SiteManagement;
using SemBroncaAI.Garage.Domain.Entities.SiteManagement;
using SemBroncaAI.Garage.Infrastructure;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Integration;
public sealed class ManagedSitesPostgresTests
{
 private static readonly string? ConnectionString=Environment.GetEnvironmentVariable("SBGARAGE_INTEGRATION_CONNECTION");
 [PostgresFact,Trait("Category","PostgreSQLIntegration")]
 public async Task Crud_dashboard_financials_history_and_archive_should_be_persisted()
 {
  await using var provider=Provider();Guid id=default;
  try{await using var scope=provider.CreateAsyncScope();var service=scope.ServiceProvider.GetRequiredService<IManagedSiteAdministration>();var command=new ManagedSiteSaveCommand{TradeName="Cliente QA",ProjectName="Site QA",Domain=$"qa-{Guid.NewGuid():N}.test",Status=ManagedSiteStatus.InDevelopment,MonthlyFee=1000,EstimatedRecurringCost=100,HostingCost=120,HostingPeriodicity=ManagedSitePeriodicity.Annual,EmailCost=60,EmailPeriodicity=ManagedSitePeriodicity.Semiannual,FinancialStatus=ManagedSiteFinancialStatus.Pending,ContractStatus=ManagedSiteContractStatus.AwaitingSignature,MonthlySupportHours=5,Costs=[new("Licença","Licença","Fornecedor",120,ManagedSitePeriodicity.Annual,DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),null)],Mailboxes=[new("contato@example.com","Cliente",ManagedSiteMailboxType.Mailbox,ManagedSiteResourceStatus.Active,null)],SupportEntries=[new(DateTime.UtcNow,"Correção","Ajuste",2,false,0,"Concluído",null)]};id=await service.CreateAsync(command,default);await service.AddHistoryAsync(id,"DNS configurado.",default);var details=await service.GetAsync(id,default);details.ShouldNotBeNull();details.RecurringCost.ShouldBe(130m);details.RecurringMargin.ShouldBe(870m);details.MarginPercent.ShouldBe(87m);details.SupportHoursRemaining.ShouldBe(3m);details.History.Count.ShouldBe(2);var dashboard=await service.DashboardAsync(new(null,null,null,null,true),default);dashboard.Items.ShouldContain(x=>x.Id==id);dashboard.PendingItems.ShouldBeGreaterThanOrEqualTo(1);dashboard.UpcomingRenewals.ShouldBeGreaterThanOrEqualTo(1);(await service.SetActiveAsync(id,false,default)).ShouldBeTrue();(await service.DashboardAsync(new(null,null,null,null,true),default)).Items.ShouldNotContain(x=>x.Id==id);}
  finally{if(id!=Guid.Empty){await using var scope=provider.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<GarageDbContext>().ManagedSites.Where(x=>x.Id==id).ExecuteDeleteAsync();}}
 }
 [PostgresFact,Trait("Category","PostgreSQLIntegration")]
 public async Task Backend_should_reject_negative_values(){await using var provider=Provider();await using var scope=provider.CreateAsyncScope();var service=scope.ServiceProvider.GetRequiredService<IManagedSiteAdministration>();await Should.ThrowAsync<ArgumentException>(()=>service.CreateAsync(new(){TradeName="Inválido",ProjectName="Inválido",Domain="invalid.test",MonthlyFee=-1},default));}
 private static ServiceProvider Provider(){var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:DefaultConnection",ConnectionString}}).Build();var services=new ServiceCollection();services.AddLogging();services.AddSingleton<IConfiguration>(configuration);services.AddSingleton<ICurrentUser>(new CurrentUser());services.AddInfrastructure(configuration);return services.BuildServiceProvider();}
 private sealed class CurrentUser:ICurrentUser{public Guid UserId{get;}=Guid.CreateVersion7();public Guid? GarageId=>null;public bool IsAuthenticated=>true;public bool IsPlatformAdmin=>true;public IReadOnlyCollection<string> Roles{get;}=[ApplicationRoles.PlatformAdmin];}
}
