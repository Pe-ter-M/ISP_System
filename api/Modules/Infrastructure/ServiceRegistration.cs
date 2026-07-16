using InternetProvider.Api.Modules.Organization.Core;
using InternetProvider.Api.Modules.Settings.Core;
using InternetProvider.Api.Modules.Users.Core;
using InternetProvider.Api.Modules.Customers.Core;
using InternetProvider.Api.Modules.Plans.Core;
using InternetProvider.Api.Modules.Subscriptions.Core;
using InternetProvider.Api.Modules.Radius.Core;
using InternetProvider.Api.Modules.Nas.Core;
using InternetProvider.Api.Modules.RadAcct.Core;
using InternetProvider.Api.Modules.RadPostAuth.Core;
using InternetProvider.Api.Modules.Roles.Core;
using InternetProvider.Api.Modules.Auth.Core;

namespace InternetProvider.Api.Modules.Infrastructure.Core;

public static class ServiceRegistration
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        AuthEndpoints.Map(app);
        OrganizationEndpoints.Map(app);
        SettingsEndpoints.Map(app);
        UserEndpoints.Map(app);
        RoleEndpoints.Map(app);
        CustomerEndpoints.Map(app);
        PlanEndpoints.Map(app);
        SubscriptionEndpoints.Map(app);
        RadiusEndpoints.Map(app);
        NasEndpoints.Map(app);
        RadAcctEndpoints.Map(app);
        RadPostAuthEndpoints.Map(app);
    }
}
