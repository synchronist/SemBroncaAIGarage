using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using SemBroncaAI.Garage.Api.Services;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using Shouldly;
using System.Text.Json;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void Owner_should_have_every_tenant_permission() =>
        RolePermissionDefaults.ForRoles([RolePermissionDefaults.Owner])
            .ShouldBe(ApplicationPermissions.All, ignoreOrder: true);

    [Fact]
    public void Receptionist_defaults_should_match_commercial_profile()
    {
        var permissions = RolePermissionDefaults.ForRoles([RolePermissionDefaults.Receptionist]);
        permissions.ShouldContain(ApplicationPermissions.ManageCustomersVehicles);
        permissions.ShouldContain(ApplicationPermissions.CreateServiceOrder);
        permissions.ShouldContain(ApplicationPermissions.ManageEstimates);
        permissions.ShouldContain(ApplicationPermissions.DeliverServiceOrder);
        permissions.ShouldContain(ApplicationPermissions.ArchiveServiceOrder);
        permissions.ShouldContain(ApplicationPermissions.CancelServiceOrder);
        permissions.ShouldNotContain(ApplicationPermissions.ManageDiagnosis);
        permissions.ShouldNotContain(ApplicationPermissions.StartService);
        permissions.ShouldNotContain(ApplicationPermissions.FinishService);
        permissions.ShouldNotContain(ApplicationPermissions.ManageGarageSettings);
        permissions.ShouldNotContain(ApplicationPermissions.ManageTeam);
        permissions.ShouldNotContain(ApplicationPermissions.ViewSubscription);
    }

    [Fact]
    public void Mechanic_defaults_should_match_technical_profile_without_financial_values()
    {
        var permissions = RolePermissionDefaults.ForRoles([RolePermissionDefaults.Mechanic]);
        permissions.ShouldContain(ApplicationPermissions.ViewServiceOrders);
        permissions.ShouldContain(ApplicationPermissions.ManageDiagnosis);
        permissions.ShouldContain(ApplicationPermissions.StartService);
        permissions.ShouldContain(ApplicationPermissions.ChangeServiceExecutionStatus);
        permissions.ShouldContain(ApplicationPermissions.FinishService);
        permissions.ShouldNotContain(ApplicationPermissions.ViewCustomersVehicles);
        permissions.ShouldNotContain(ApplicationPermissions.ManageCustomersVehicles);
        permissions.ShouldNotContain(ApplicationPermissions.CreateServiceOrder);
        permissions.ShouldNotContain(ApplicationPermissions.ViewEstimateValues);
        permissions.ShouldNotContain(ApplicationPermissions.ManageEstimates);
        permissions.ShouldNotContain(ApplicationPermissions.DeliverServiceOrder);
        permissions.ShouldNotContain(ApplicationPermissions.CancelServiceOrder);
        permissions.ShouldNotContain(ApplicationPermissions.ArchiveServiceOrder);
        permissions.ShouldNotContain(ApplicationPermissions.ManageGarageSettings);
        permissions.ShouldNotContain(ApplicationPermissions.ManageTeam);
        permissions.ShouldNotContain(ApplicationPermissions.ViewSubscription);
    }

    [Fact]
    public void Platform_admin_should_not_receive_tenant_permissions() =>
        RolePermissionDefaults.ForRoles(["PlatformAdmin"]).ShouldBeEmpty();

    [Theory]
    [InlineData(nameof(ServiceOrdersController.SaveDiagnosis), ApplicationPermissions.ManageDiagnosis)]
    [InlineData(nameof(ServiceOrdersController.SaveEstimate), ApplicationPermissions.ManageEstimates)]
    [InlineData(nameof(ServiceOrdersController.SendForApproval), ApplicationPermissions.SendEstimateForApproval)]
    [InlineData(nameof(ServiceOrdersController.StartService), ApplicationPermissions.StartService)]
    [InlineData(nameof(ServiceOrdersController.Finish), ApplicationPermissions.FinishService)]
    [InlineData(nameof(ServiceOrdersController.Deliver), ApplicationPermissions.DeliverServiceOrder)]
    [InlineData(nameof(ServiceOrdersController.Cancel), ApplicationPermissions.CancelServiceOrder)]
    public void Sensitive_service_order_endpoint_should_require_permission(string method, string permission) =>
        typeof(ServiceOrdersController).GetMethod(method)!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(permission);

    [Fact]
    public void Financial_central_should_require_view_values_permission() =>
        typeof(EstimatesController).GetMethod(nameof(EstimatesController.List))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(ApplicationPermissions.ViewEstimateValues);

    [Fact]
    public void Technical_history_endpoint_should_require_technical_permission() =>
        typeof(ServiceOrdersController).GetMethod(nameof(ServiceOrdersController.GetTechnicalHistory))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(ApplicationPermissions.ManageDiagnosis);

    [Fact]
    public void Administrative_settings_should_require_manage_permission_while_context_remains_tenant_scoped()
    {
        typeof(GaragesController).GetMethod(nameof(GaragesController.GetSettings))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(ApplicationPermissions.ManageGarageSettings);
        typeof(GaragesController).GetMethod(nameof(GaragesController.GetContext))!
            .GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe(ApplicationPermissions.ViewEstimateValues);
        typeof(GaragesController).GetMethod(nameof(GaragesController.GetBranding))!
            .GetCustomAttribute<AuthorizeAttribute>().ShouldBeNull();
        typeof(GaragesController).GetCustomAttribute<AuthorizeAttribute>()!.Policy.ShouldBe("TenantUser");
        var properties = typeof(SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings.GetGarageContextResponse)
            .GetProperties().Select(item => item.Name).ToArray();
        properties.ShouldNotContain("Id");
        properties.ShouldNotContain("Active");
        properties.ShouldNotContain("CreatedAt");
        typeof(SemBroncaAI.Garage.Application.Features.Garages.GetGarageSettings.GetGarageBrandingResponse)
            .GetProperties().Select(item => item.Name)
            .ShouldBe(["Name", "City", "State", "LogoStorageKey", "PrimaryColor"], ignoreOrder: true);
    }

    [Fact]
    public void Lookup_should_require_service_order_creation_permission() =>
        typeof(LookupController).GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(ApplicationPermissions.CreateServiceOrder);

    [Fact]
    public void Mechanic_read_model_should_keep_workflow_state_without_financial_or_internal_data()
    {
        var response = CreateServiceOrderResponse();

        var filtered = ServiceOrderResponseAuthorization.Filter(
            response,
            canViewEstimateValues: false,
            canManageDiagnosis: true,
            canViewCustomerPersonalData: false);

        filtered.Estimate.ShouldBeNull();
        filtered.Approval.ShouldNotBeNull();
        filtered.Approval.Status.ShouldBe(EstimateApprovalStatus.Approved);
        filtered.Approval.Token.ShouldBeNull();
        filtered.Approval.CustomerName.ShouldBeNull();
        filtered.Approval.CustomerComment.ShouldBeNull();
        filtered.ApprovalHistory.ShouldBeEmpty();
        filtered.Diagnosis!.InternalNotes.ShouldBe("nota interna");
        filtered.TechnicalHistory.Count.ShouldBe(1);
        filtered.TechnicalHistory.Single().WorkItems.ShouldBe(["Troca de óleo"]);
        filtered.Customer.Name.ShouldBe("Cliente");
        filtered.Customer.Document.ShouldBeNull();
        filtered.Customer.Phone.ShouldBeNull();
        filtered.Customer.Email.ShouldBeNull();
        var json = JsonSerializer.Serialize(filtered);
        json.ShouldNotContain("Document");
        json.ShouldNotContain("Phone");
        json.ShouldNotContain("Email");
    }

    [Fact]
    public void Mechanic_service_order_list_should_not_return_customer_phone()
    {
        var item = new ListServiceOrdersItem(
            Guid.NewGuid(), 1, ServiceOrderStatus.Diagnosis, null, DateTimeOffset.UtcNow,
            "relato", Guid.NewGuid(), "Cliente", "11999999999", Guid.NewGuid(),
            "ABC1D23", "Marca", "Modelo", "Versão", 2025);

        var filtered = ServiceOrderResponseAuthorization.Filter(
            new ListServiceOrdersResponse(1, 20, 1, 1, [item]),
            canViewCustomerPersonalData: false);

        filtered.Items.Single().CustomerName.ShouldBe("Cliente");
        filtered.Items.Single().CustomerPhone.ShouldBeNull();
        JsonSerializer.Serialize(filtered).ShouldNotContain("CustomerPhone");
    }

    [Fact]
    public void Receptionist_read_model_should_hide_internal_notes_but_keep_financial_data()
    {
        var filtered = ServiceOrderResponseAuthorization.Filter(
            CreateServiceOrderResponse(),
            canViewEstimateValues: true,
            canManageDiagnosis: false,
            canViewCustomerPersonalData: true);

        filtered.Estimate.ShouldNotBeNull();
        filtered.Approval!.Token.ShouldBe("token-protegido");
        filtered.Diagnosis!.Description.ShouldBe("diagnóstico");
        filtered.Diagnosis.InternalNotes.ShouldBeEmpty();
        filtered.Customer.Document.ShouldBe("doc");
    }

    [Theory]
    [InlineData(typeof(CustomersModuleController), nameof(CustomersModuleController.List))]
    [InlineData(typeof(CustomersModuleController), nameof(CustomersModuleController.GetById))]
    [InlineData(typeof(VehiclesController), nameof(VehiclesController.List))]
    [InlineData(typeof(VehiclesController), nameof(VehiclesController.GetById))]
    public void Administrative_customer_and_vehicle_reads_should_require_the_administrative_view_permission(
        Type controller, string method) =>
        controller.GetMethod(method)!.GetCustomAttribute<AuthorizeAttribute>()!.Policy
            .ShouldBe(ApplicationPermissions.ViewCustomersVehicles);

    [Fact]
    public void Technical_history_contract_should_not_contain_financial_or_customer_contact_fields()
    {
        var names = typeof(ServiceOrderTechnicalHistoryResponse).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        names.ShouldContain("Diagnosis");
        names.ShouldContain("InternalNotes");
        names.ShouldContain("WorkItems");
        names.ShouldNotContain("Total");
        names.ShouldNotContain("Estimate");
        names.ShouldNotContain("Phone");
        names.ShouldNotContain("Email");
        names.ShouldNotContain("Document");
    }

    private static GetServiceOrderByIdResponse CreateServiceOrderResponse()
    {
        var now = DateTimeOffset.UtcNow;
        return new GetServiceOrderByIdResponse(
            Guid.NewGuid(), Guid.NewGuid(), 1, ServiceOrderStatus.WaitingApproval,
            "relato", 100, now,
            new ServiceOrderCustomerResponse(Guid.NewGuid(), "Cliente", "doc", "fone", "email"),
            new ServiceOrderVehicleResponse(Guid.NewGuid(), "ABC1D23", "Marca", "Modelo", "Versão", 2025, "Cor", "Flex", 100),
            new ServiceOrderDiagnosisResponse(Guid.NewGuid(), "diagnóstico", "nota interna", Guid.NewGuid(), now, now),
            new ServiceOrderEstimateResponse(Guid.NewGuid(), 100, 50, 150, now, now, []),
            new ApprovalSummaryResponse(EstimateApprovalStatus.Approved, now, now.AddDays(7), now, "Cliente", "comentário", "token-protegido"),
            [new ApprovalHistoryResponse(EstimateApprovalStatus.Approved, now, now.AddDays(7), now, null, "Cliente", "comentário")],
            [],
            [new ServiceOrderTechnicalHistoryResponse(
                Guid.NewGuid(), 2, ServiceOrderStatus.Finished, "relato anterior", 90,
                now.AddMonths(-1), "diagnóstico anterior", "nota anterior", ["Troca de óleo"])],
            null);
    }
}
