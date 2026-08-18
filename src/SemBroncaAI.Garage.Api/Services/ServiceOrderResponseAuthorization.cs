using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

namespace SemBroncaAI.Garage.Api.Services;

public static class ServiceOrderResponseAuthorization
{
    public static ListServiceOrdersResponse Filter(
        ListServiceOrdersResponse response,
        bool canViewCustomerPersonalData) =>
        canViewCustomerPersonalData
            ? response
            : response with
            {
                Items = response.Items
                    .Select(item => item with { CustomerPhone = null })
                    .ToArray()
            };

    public static GetServiceOrderByIdResponse Filter(
        GetServiceOrderByIdResponse response,
        bool canViewEstimateValues,
        bool canManageDiagnosis,
        bool canViewCustomerPersonalData)
    {
        if (!canViewCustomerPersonalData)
            response = response with
            {
                Customer = response.Customer with
                {
                    Document = null,
                    Phone = null,
                    Email = null
                }
            };

        if (!canViewEstimateValues)
            response = response with
            {
                Estimate = null,
                Approval = response.Approval is null
                    ? null
                    : response.Approval with
                    {
                        CustomerName = null,
                        CustomerComment = null,
                        Token = null
                    },
                ApprovalHistory = []
            };

        if (response.Diagnosis is not null && !canManageDiagnosis)
            response = response with
            {
                Diagnosis = response.Diagnosis with { InternalNotes = string.Empty }
            };

        return response;
    }
}
