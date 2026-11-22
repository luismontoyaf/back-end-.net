namespace Core.Models
{
    public sealed class CreatorLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class CreatorLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public sealed class InitializeSubscriptionRequest
    {
        public DateTime StartDate { get; set; }
    }

    public sealed class RegisterSubscriptionPaymentRequest
    {
        public DateTime? PaidAt { get; set; }
    }

    public sealed class SubscriptionCompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = CompanyPlan.Standard;
        public bool IsActive { get; set; }
        public bool SuspendedForOverdue { get; set; }
        public DateTime? StartDate { get; set; }
        public List<SubscriptionPeriodDto> Periods { get; set; } = [];
        public List<CompanyBranchDto> Branches { get; set; } = [];
    }

    public sealed class CompanyBranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CompanyNit { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsHeadquarters { get; set; }
    }

    public sealed class CreateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string BranchNit { get; set; } = string.Empty;
        public string BranchIdentifier { get; set; } = string.Empty;
        public string BranchAddress { get; set; } = string.Empty;
        public string BranchPhone { get; set; } = string.Empty;
        public string BranchEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string AdminFirstName { get; set; } = string.Empty;
        public string AdminLastName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminDocumentType { get; set; } = string.Empty;
        public string AdminDocumentNumber { get; set; } = string.Empty;
        public string AdminPhone { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
    }

    public sealed class CompanySettingsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = CompanyPlan.Standard;
        public string? UpgradeRequestStatus { get; set; }
        public DateTime? UpgradeRequestedAt { get; set; }
    }

    public sealed class UpdateCompanySettingsRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public sealed class PlanUpgradeRequestDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyNit { get; set; } = string.Empty;
        public string RequestedByEmail { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public sealed class CreateBranchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public sealed class SwitchBranchRequest
    {
        public int BranchId { get; set; }
    }

    public sealed class SubscriptionPeriodDto
    {
        public int Id { get; set; }
        public int CycleNumber { get; set; }
        public int MonthNumber { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ReminderCount { get; set; }
        public DateTime? LastReminderSentAt { get; set; }
    }
}
