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

    public sealed class SubscriptionTenantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool SuspendedForOverdue { get; set; }
        public DateTime? StartDate { get; set; }
        public List<SubscriptionPeriodDto> Periods { get; set; } = [];
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
