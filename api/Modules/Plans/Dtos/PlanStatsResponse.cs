namespace InternetProvider.Api.Modules.Plans.Dtos;

public class PlanStatsResponse
{
    public int TotalPlans { get; set; }
    public int ActivePlans { get; set; }
    public int InactivePlans { get; set; }
    public int TotalSubscribers { get; set; }
}
