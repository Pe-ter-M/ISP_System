namespace InternetProvider.Api.Modules.Customers.Dtos;

/// <summary>Outcome of a customer delete. HardDeleted=false means the account was deactivated to preserve history.</summary>
public record DeleteCustomerResult(
    bool HardDeleted,
    string Message
);
