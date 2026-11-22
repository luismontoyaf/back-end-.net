using Core.Models;

namespace Core.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<List<Company>> GetAllCompaniesAsync();
        Task<List<TenantSubscriptionPeriod>> GetAllPeriodsAsync();
        Task<Company?> GetCompanyByIdAsync(int companyId);
        Task<List<(TenantSubscriptionPeriod Period, Company Company)>> GetDueReminderCandidatesAsync(DateTime today, DateTime now, DateTime reminderCutoff);
        Task<bool> TryClaimReminderAsync(int periodId, DateTime today, DateTime now, DateTime reminderCutoff);
        Task<HashSet<string>> GetDeliveredReminderRecipientsAsync(int periodId, int reminderNumber);
        Task RecordReminderDeliveryAsync(int periodId, int reminderNumber, string recipientEmail, DateTime sentAt);
        Task CompleteReminderAsync(int periodId, DateTime sentAt);
        Task ReleaseReminderClaimAsync(int periodId);
        Task<bool> InitializeSubscriptionAsync(int companyId, DateTime startDate, DateTime createdAt);
        Task<bool> RecordPaymentAsync(int periodId, DateTime paidAt, string recordedBy, DateTime today);
        Task<bool> DeactivateCompanyAsync(int companyId, DateTime overdueBefore);
        Task<bool> ActivateCompanyAsync(int companyId);
        Task<bool> RenewSubscriptionAsync(int companyId, DateTime createdAt);
    }
}
