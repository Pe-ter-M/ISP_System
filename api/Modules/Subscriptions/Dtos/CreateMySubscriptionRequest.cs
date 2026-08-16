using InternetProvider.Api.Modules.Payments.Core;

namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

public record CreateMySubscriptionRequest(
    int PackageId,
    bool? AutoRenew,
    PaymentMethod PaymentMethod,
    string PhoneNumber,
    string? ReferenceNotes
);