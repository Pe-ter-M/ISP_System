using InternetProvider.Api.Modules.Payments.Core;

namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

public record CreateSubscriptionRequest(
    int CustomerId,
    int PackageId,
    string Username,
    string Password,
    bool? AutoRenew,
    
    // ── Payment Dynamic Parameters ──
    PaymentMethod PaymentMethod, // Enum constraint to avoid case-sensitivity issues
    string PhoneNumber,          // The cellular billing line
    string? ReferenceNotes       // Optional details or client message
);