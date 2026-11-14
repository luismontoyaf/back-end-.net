using Core.Models;

namespace Core.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<List<Tenant>> GetAllTenantsAsync();
        Task<List<TenantSubscriptionPeriod>> GetAllPeriodsAsync();
        Task<Tenant?> GetTenantByIdAsync(int tenantId);
        Task<List<(TenantSubscriptionPeriod Period, Tenant Tenant)>> GetDueReminderCandidatesAsync(DateTime today, DateTime now, DateTime reminderCutoff);
        Task<bool> TryClaimReminderAsync(int periodId, DateTime today, DateTime now, DateTime reminderCutoff);
        Task<HashSet<string>> GetDeliveredReminderRecipientsAsync(int periodId, int reminderNumber);
        Task RecordReminderDeliveryAsync(int periodId, int reminderNumber, string recipientEmail, DateTime sentAt);
        Task CompleteReminderAsync(int periodId, DateTime sentAt);
        Task ReleaseReminderClaimAsync(int periodId);
        Task<bool> InitializeSubscriptionAsync(int tenantId, DateTime startDate, DateTime createdAt);
        Task<bool> RecordPaymentAsync(int periodId, DateTime paidAt, string recordedBy, DateTime today);
        Task<bool> DeactivateTenantAsync(int tenantId, DateTime overdueBefore);
        Task<bool> ActivateTenantAsync(int tenantId);
        Task<bool> RenewSubscriptionAsync(int tenantId, DateTime createdAt);
    }
}
