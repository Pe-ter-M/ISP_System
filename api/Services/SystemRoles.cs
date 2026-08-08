namespace InternetProvider.Api.Services;

public enum SystemRole
{
    Admin,
    Secretary,
    HeadTechnician,
    FieldTechnician,
    Customer
}

public static class RoleNames
{
    // Maps enum values to the exact name strings stored in the roles table
    private static readonly Dictionary<SystemRole, string> _names = new()
    {
        { SystemRole.Admin,           "Admin" },
        { SystemRole.Secretary,       "Secretary" },
        { SystemRole.HeadTechnician,  "Head Technician" },
        { SystemRole.FieldTechnician, "Field Technician" },
        { SystemRole.Customer,        "Customer" },
    };

    public static string Of(SystemRole role) => _names[role];
}
