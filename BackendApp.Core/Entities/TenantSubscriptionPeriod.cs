namespace Core.Models
{
    public class TenantSubscriptionPeriod
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        // Retained for legacy reporting; new billing is owned by the company.
        public int TenantId { get; set; }
        public int CycleNumber { get; set; }
        public int MonthNumber { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentRecordedBy { get; set; }
        public DateTime? LastReminderSentAt { get; set; }
        public int ReminderCount { get; set; }
        public DateTime? ReminderClaimedUntil { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TenantSubscriptionReminderDelivery
    {
        public int Id { get; set; }
        public int PeriodId { get; set; }
        public int ReminderNumber { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
