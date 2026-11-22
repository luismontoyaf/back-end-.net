namespace Core.Models
{
    public static class CompanyPlan
    {
        public const string Standard = "standard";
        public const string Plus = "plus";

        public static bool IsValid(string? plan) =>
            string.Equals(plan, Standard, StringComparison.OrdinalIgnoreCase)
            || string.Equals(plan, Plus, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = CompanyPlan.Standard;
        public bool IsActive { get; set; } = true;
        public bool SuspendedForOverdue { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Tenant> Branches { get; set; } = new List<Tenant>();
    }

    public static class PlanUpgradeRequestStatus
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
    }

    public sealed class CompanyPlanUpgradeRequest
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string RequestedByEmail { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = PlanUpgradeRequestStatus.Pending;
        public DateTime? DecidedAt { get; set; }
        public string? DecidedByEmail { get; set; }
        public Company Company { get; set; } = null!;
    }
}
