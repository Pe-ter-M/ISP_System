namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

public class SubscriptionStatsResponse
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Suspended { get; set; }
    public int Expired { get; set; }
}
