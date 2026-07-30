namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

public record CreateSubscriptionRequest(
    int CustomerId,
    int PackageId,
    string Username,
    string Password,
    bool? AutoRenew,
    
    // ── Payment Dynamic Parameters ──
    string PaymentMethod,     // e.g. "Mock", "Mpesa", "Airtel" (dynamic billing choice)
    string PhoneNumber,      // The cellular billing line
    string? ReferenceNotes   // Optional details or client message
);