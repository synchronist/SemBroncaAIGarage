using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemBroncaAI.Garage.Api.Controllers;
using SemBroncaAI.Garage.Application.Common;
using SemBroncaAI.Garage.Application.Features.PlatformAdministration;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Application.Abstractions.Security;
using Shouldly;
using SemBroncaAI.Garage.Infrastructure.Services;
using SemBroncaAI.Garage.Web.Services;
using System.ComponentModel.DataAnnotations;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemBroncaAI.Garage.Tests.Api;

public sealed class PlatformAdministrationTests
{
    [Fact]
    public void Administrative_boundary_should_require_only_platform_admin_role()
    {
        var authorization = typeof(PlatformGaragesController).GetCustomAttribute<AuthorizeAttribute>();
        authorization.ShouldNotBeNull();
        authorization.Policy.ShouldBe(PlatformAuthorization.Policy);
        authorization.Roles.ShouldBeNull();
        typeof(PlatformGaragesController).GetCustomAttribute<AllowAnonymousAttribute>().ShouldBeNull();
    }

    [Fact]
    public void Administrative_contracts_should_not_expose_password_or_operational_data()
    {
        var properties = new[] { typeof(PlatformGarageListItem), typeof(PlatformGarageDetailsResponse),
            typeof(CreatePlatformGarageResponse), typeof(PlatformDashboardResponse) }
            .SelectMany(type => type.GetProperties()).Select(property => property.Name).ToArray();

        properties.ShouldNotContain(name => name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        properties.ShouldNotContain(name =>
            new[] { "Customers", "Vehicles", "ServiceOrders", "Estimates" }.Contains(name));
    }

    [Fact]
    public async Task List_endpoint_should_forward_server_side_pagination_search_and_active_filter()
    {
        var administration = new FakeAdministration();
        var controller = new PlatformGaragesController(administration);

        var result = await controller.List("Oficina", false, 3, 25, default);

        result.Result.ShouldBeOfType<OkObjectResult>();
        administration.ListQuery.ShouldBe(new ListPlatformGaragesQuery("Oficina", false, 3, 25));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Administrative_pagination_should_reject_invalid_limits(int page, int pageSize) =>
        Should.Throw<ArgumentOutOfRangeException>(() => PaginationRules.Validate(page, pageSize));

    [Fact]
    public async Task Create_endpoint_should_return_only_safe_onboarding_confirmation()
    {
        var administration = new FakeAdministration();
        var controller = new PlatformGaragesController(administration);
        var command = new CreatePlatformGarageCommand("Oficina", "123", "1199", "garage@test.local",
            "Owner", "owner@test.local", "owner", "Initial123", "Initial123");

        var result = await controller.Create(command, default);

        result.Result.ShouldBeOfType<CreatedAtActionResult>();
        administration.CreateCommand.ShouldBe(command);
        typeof(CreatePlatformGarageResponse).GetProperties().Select(x => x.Name)
            .ShouldBe(["GarageId", "Name", "Active"]);
    }

    [Fact]
    public async Task Status_endpoint_should_change_only_requested_garage_status()
    {
        var administration = new FakeAdministration();
        var controller = new PlatformGaragesController(administration);
        var garageId = Guid.NewGuid();

        (await controller.SetActive(garageId, new SetGarageActiveRequest(false), default))
            .ShouldBeOfType<NoContentResult>();
        administration.StatusChange.ShouldBe((garageId, false));
    }

    [Fact]
    public async Task Subscription_endpoint_should_remain_inside_platform_admin_boundary()
    {
        var administration = new FakeAdministration();
        var controller = new PlatformGaragesController(administration);
        var garageId = Guid.NewGuid();
        var command = new UpdateGarageSubscriptionCommand(SubscriptionStatus.Suspended, SubscriptionPlan.Standard);

        var result = await controller.UpdateSubscription(garageId, command, default);

        result.Result.ShouldBeOfType<OkObjectResult>();
        administration.SubscriptionChange.ShouldBe((garageId, command));
    }

    [Fact]
    public void Trial_should_use_configured_duration_and_identify_expiration()
    {
        var now = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 14);

        subscription.Status.ShouldBe(SubscriptionStatus.Trial);
        subscription.TrialEndsAt.ShouldBe(now.AddDays(14));
        subscription.IsTrialExpired(now.AddDays(15)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Active)]
    public void Subscription_should_support_central_administrative_statuses(SubscriptionStatus status)
    {
        var now = DateTime.UtcNow;
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 14);
        subscription.ChangeStatus(status, now.AddMinutes(1), now.AddDays(20));
        subscription.Status.ShouldBe(status);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Suspended, false)]
    [InlineData(SubscriptionStatus.Cancelled, false)]
    [InlineData(SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Trial, true)]
    [InlineData(SubscriptionStatus.PastDue, null)]
    public void Subscription_operational_policy_should_be_centralized(SubscriptionStatus status, bool? expected) =>
        SubscriptionOperationalPolicy.RequiredGarageActive(status).ShouldBe(expected);

    [Fact]
    public void Subscription_plan_should_be_administratively_changeable()
    {
        var now = DateTime.UtcNow;
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 14);
        subscription.ChangePlan(SubscriptionPlan.Standard, now.AddMinutes(1));
        subscription.Plan.ShouldBe(SubscriptionPlan.Standard);
        new SubscriptionOptions().DefaultTrialDays.ShouldBe(10);
    }

    [Fact]
    public void Domain_should_reject_returning_an_existing_subscription_to_trial()
    {
        var now = DateTime.UtcNow;
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 10);
        subscription.ChangeStatus(SubscriptionStatus.Active, now.AddMinutes(1));
        Should.Throw<InvalidOperationException>(() =>
            subscription.ChangeStatus(SubscriptionStatus.Trial, now.AddMinutes(2)));
    }

    [Fact]
    public void Reactivation_should_preserve_cancellation_date()
    {
        var now = DateTime.UtcNow;
        var subscription = new GarageSubscriptionEntity(Guid.NewGuid(), SubscriptionPlan.Standard, now, 10);
        subscription.ChangeStatus(SubscriptionStatus.Cancelled, now.AddMinutes(1));
        var cancelledAt = subscription.CancelledAt;
        subscription.ChangeStatus(SubscriptionStatus.Active, now.AddMinutes(2));
        subscription.CancelledAt.ShouldBe(cancelledAt);
    }

    [Fact]
    public async Task Web_contract_should_deserialize_string_subscription_statuses_in_list_and_details()
    {
        var now = DateTime.UtcNow;
        var active = new PlatformGarageListItem(Guid.NewGuid(), "Ativa", "1", "a@test.local", "11999999999",
            true, now, 1, "Owner", "owner@test.local", SubscriptionStatus.Active);
        var suspended = active with { Id = Guid.NewGuid(), Name = "Suspensa", Active = false,
            SubscriptionStatus = SubscriptionStatus.Suspended };
        var list = new PlatformGarageListResponse(1, 20, 2, 1, [active, suspended]);
        var details = new PlatformGarageDetailsResponse(active.Id, active.Name, active.Document, active.Email,
            active.Phone, active.Active, now, 1, active.OwnerName, active.OwnerEmail, "owner",
            new(SubscriptionStatus.Active, SubscriptionPlan.Standard, now, null, null, null, null, null, false));
        var handler = new JsonResponseHandler(list, details);
        var service = new PlatformGarageService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await service.ListAsync(null, null, 1, 20);
        var loadedDetails = await service.GetAsync(active.Id);

        result.Items.Select(x => x.SubscriptionStatus).ShouldBe([SubscriptionStatus.Active, SubscriptionStatus.Suspended]);
        loadedDetails.ShouldNotBeNull().Subscription.Status.ShouldBe(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Web_service_should_return_null_only_for_real_not_found_and_propagate_contract_errors()
    {
        var notFound = new PlatformGarageService(new HttpClient(new StatusHandler(HttpStatusCode.NotFound, ""))
            { BaseAddress = new Uri("http://localhost/") });
        (await notFound.GetAsync(Guid.NewGuid())).ShouldBeNull();

        var malformed = new PlatformGarageService(new HttpClient(new StatusHandler(HttpStatusCode.OK, "{ invalid"))
            { BaseAddress = new Uri("http://localhost/") });
        await Should.ThrowAsync<JsonException>(() => malformed.GetAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.PastDue, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Active, false)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.PastDue, false)]
    [InlineData(SubscriptionStatus.Trial, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.Suspended, true)]
    [InlineData(SubscriptionStatus.Cancelled, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Cancelled, SubscriptionStatus.Trial, false)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Trial, false)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.Trial, false)]
    [InlineData(SubscriptionStatus.Suspended, SubscriptionStatus.Trial, false)]
    public void Only_valid_subscription_transitions_should_be_offered(
        SubscriptionStatus current, SubscriptionStatus target, bool expected) =>
        SubscriptionTransitions.IsAllowed(current, target).ShouldBe(expected);

    [Fact]
    public void Empty_onboarding_should_return_safe_portuguese_errors_by_field()
    {
        var exception = Validate(new CreatePlatformGarageCommand("", "", "", "invalid", "", "invalid", "", "weak", "different"));

        exception.Errors.Keys.ShouldContain("name");
        exception.Errors.Keys.ShouldContain("phone");
        exception.Errors.Keys.ShouldContain("ownerEmail");
        exception.Errors.Keys.ShouldContain("initialPassword");
        exception.Errors.Keys.ShouldContain("confirmPassword");
        var messages = string.Join(" ", exception.Errors.SelectMany(x => x.Value));
        messages.ShouldContain("Informe o nome da oficina.");
        messages.ShouldContain("Informe um e-mail válido.");
        messages.ShouldContain("As senhas não coincidem.");
        messages.ShouldNotContain("maximum length");
        messages.ShouldNotContain("InitialPassword");
        messages.ShouldNotContain("OwnerName");
    }

    [Fact]
    public async Task Validation_contract_should_preserve_field_errors_without_internal_details()
    {
        var administration = new FakeAdministration
        {
            CreateException = new PlatformGarageValidationException(new Dictionary<string, string[]>
            {
                ["ownerEmail"] = ["Informe um e-mail válido."]
            })
        };

        var result = await new PlatformGaragesController(administration).Create(
            new("Garage", "123", "15999999999", "garage@test.local", "Owner", "invalid", "owner", "ValidPass1", "ValidPass1"), default);

        var response = result.Result.ShouldBeOfType<BadRequestObjectResult>().Value
            .ShouldBeOfType<PlatformGarageValidationErrorResponse>();
        response.Errors["ownerEmail"].ShouldBe(["Informe um e-mail válido."]);
        response.Message.ShouldBe("Revise os campos destacados abaixo.");
    }

    [Theory]
    [InlineData("(15) 3232-1234", "1532321234")]
    [InlineData("(15) 99999-9999", "15999999999")]
    public void Brazilian_phone_should_be_valid_and_normalized(string input, string normalized)
    {
        PlatformGarageInputRules.IsValidPhone(input).ShouldBeTrue();
        PlatformGarageInputRules.NormalizePhone(input).ShouldBe(normalized);
    }

    [Fact]
    public void Numeric_document_should_be_normalized_without_rejecting_other_document_types()
    {
        PlatformGarageInputRules.NormalizeDocument("12.345.678/0001-90").ShouldBe("12345678000190");
        PlatformGarageInputRules.NormalizeDocument("DOC-EXTERNO-A1").ShouldBe("DOC-EXTERNO-A1");
    }

    [Fact]
    public void Invalid_email_should_produce_one_product_message()
    {
        var attribute = new ProductEmailAttribute(150) { ErrorMessage = "Informe um e-mail válido." };
        var results = new List<ValidationResult>();

        Validator.TryValidateValue("email-invalido", new ValidationContext(new object()), results, [attribute]);

        results.Select(result => result.ErrorMessage).ShouldBe(["Informe um e-mail válido."]);
    }

    [Fact]
    public void User_name_should_enforce_the_product_limit()
    {
        PlatformGarageInputRules.IsValidUserName(new string('a', 100)).ShouldBeTrue();
        PlatformGarageInputRules.IsValidUserName(new string('a', 101)).ShouldBeFalse();
        Validate(new CreatePlatformGarageCommand("Garage", "123", "15999999999", "garage@test.local",
            "Owner", "owner@test.local", new string('a', 101), "ValidPass1", "ValidPass1"))
            .Errors["ownerUserName"].ShouldBe(["O nome de usuário é muito longo."]);
    }

    private static PlatformGarageValidationException Validate(CreatePlatformGarageCommand command)
    {
        var method = typeof(PlatformGarageAdministration).GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static)!;
        return Should.Throw<System.Reflection.TargetInvocationException>(() => method.Invoke(null, [command]))
            .InnerException.ShouldBeOfType<PlatformGarageValidationException>();
    }

    private sealed class FakeAdministration : IPlatformGarageAdministration
    {
        public Exception? CreateException { get; init; }
        public ListPlatformGaragesQuery? ListQuery { get; private set; }
        public CreatePlatformGarageCommand? CreateCommand { get; private set; }
        public (Guid Id, bool Active)? StatusChange { get; private set; }
        public (Guid Id, UpdateGarageSubscriptionCommand Command)? SubscriptionChange { get; private set; }
        public Task<PlatformDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlatformDashboardResponse(0, 0, 0, 0, 0, 0, 0, []));
        public Task<PlatformGarageListResponse> ListAsync(ListPlatformGaragesQuery query, CancellationToken cancellationToken = default)
        {
            ListQuery = query;
            return Task.FromResult(new PlatformGarageListResponse(query.Page, query.PageSize, 0, 0, []));
        }
        public Task<PlatformGarageDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformGarageDetailsResponse?>(null);
        public Task<CreatePlatformGarageResponse> CreateAsync(CreatePlatformGarageCommand command, CancellationToken cancellationToken = default)
        {
            if (CreateException is not null) throw CreateException;
            CreateCommand = command;
            return Task.FromResult(new CreatePlatformGarageResponse(Guid.NewGuid(), command.Name, true));
        }
        public Task<bool> SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
        {
            StatusChange = (id, active);
            return Task.FromResult(true);
        }
        public Task<PlatformSubscriptionResponse?> UpdateSubscriptionAsync(Guid id,
            UpdateGarageSubscriptionCommand command, CancellationToken cancellationToken = default)
        {
            SubscriptionChange = (id, command);
            return Task.FromResult<PlatformSubscriptionResponse?>(new(command.Status, command.Plan,
                DateTime.UtcNow, null, null, null, null, null, false));
        }
    }

    private sealed class JsonResponseHandler(PlatformGarageListResponse list, PlatformGarageDetailsResponse details)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object value = request.RequestUri!.AbsolutePath.EndsWith($"/{details.Id}", StringComparison.Ordinal)
                ? details : list;
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(value, value.GetType(), options), Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class StatusHandler(HttpStatusCode status, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }
}
