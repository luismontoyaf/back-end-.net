using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public sealed class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _context;

        public SubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Company>> GetAllCompaniesAsync() =>
            _context.Companies.AsNoTracking()
                .Include(company => company.Branches)
                .OrderBy(company => company.Name)
                .ToListAsync();

        public Task<List<TenantSubscriptionPeriod>> GetAllPeriodsAsync() =>
            _context.TenantSubscriptionPeriods.AsNoTracking()
                .OrderBy(period => period.TenantId)
                .ThenBy(period => period.CycleNumber)
                .ThenBy(period => period.MonthNumber)
                .ToListAsync();

        public Task<Company?> GetCompanyByIdAsync(int companyId) =>
            _context.Companies.FirstOrDefaultAsync(company => company.Id == companyId);

        public async Task<List<(TenantSubscriptionPeriod Period, Company Company)>> GetDueReminderCandidatesAsync(
            DateTime today,
            DateTime now,
            DateTime reminderCutoff)
        {
            var delinquentBefore = today.AddDays(-3);
            var candidates = await (
                from period in _context.TenantSubscriptionPeriods.AsNoTracking()
                join company in _context.Companies.AsNoTracking() on period.CompanyId equals company.Id
                where period.PaidAt == null
                    && period.DueDate < delinquentBefore
                    && (period.LastReminderSentAt == null || period.LastReminderSentAt <= reminderCutoff)
                    && (period.ReminderClaimedUntil == null || period.ReminderClaimedUntil < now)
                orderby period.DueDate
                select new { Period = period, Company = company })
                .ToListAsync();

            return candidates.Select(candidate => (candidate.Period, candidate.Company)).ToList();
        }

        public async Task<bool> TryClaimReminderAsync(
            int periodId,
            DateTime today,
            DateTime now,
            DateTime reminderCutoff)
        {
            var delinquentBefore = today.AddDays(-3);
            var affected = await _context.TenantSubscriptionPeriods
                .Where(period => period.Id == periodId
                    && period.PaidAt == null
                    && period.DueDate < delinquentBefore
                    && (period.LastReminderSentAt == null || period.LastReminderSentAt <= reminderCutoff)
                    && (period.ReminderClaimedUntil == null || period.ReminderClaimedUntil < now))
                .ExecuteUpdateAsync(update => update
                    .SetProperty(period => period.ReminderClaimedUntil, now.AddMinutes(20)));

            return affected == 1;
        }

        public async Task<HashSet<string>> GetDeliveredReminderRecipientsAsync(int periodId, int reminderNumber)
        {
            var recipients = await _context.TenantSubscriptionReminderDeliveries.AsNoTracking()
                .Where(delivery => delivery.PeriodId == periodId && delivery.ReminderNumber == reminderNumber)
                .Select(delivery => delivery.RecipientEmail)
                .ToListAsync();
            return recipients.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task RecordReminderDeliveryAsync(
            int periodId,
            int reminderNumber,
            string recipientEmail,
            DateTime sentAt)
        {
            var alreadyRecorded = await _context.TenantSubscriptionReminderDeliveries.AnyAsync(delivery =>
                delivery.PeriodId == periodId
                && delivery.ReminderNumber == reminderNumber
                && delivery.RecipientEmail == recipientEmail);
            if (alreadyRecorded)
                return;

            _context.TenantSubscriptionReminderDeliveries.Add(new TenantSubscriptionReminderDelivery
            {
                PeriodId = periodId,
                ReminderNumber = reminderNumber,
                RecipientEmail = recipientEmail,
                SentAt = sentAt
            });
            await _context.SaveChangesAsync();
        }

        public async Task CompleteReminderAsync(int periodId, DateTime sentAt)
        {
            await _context.TenantSubscriptionPeriods
                .Where(period => period.Id == periodId)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(period => period.LastReminderSentAt, sentAt)
                    .SetProperty(period => period.ReminderCount, period => period.ReminderCount + 1)
                    .SetProperty(period => period.ReminderClaimedUntil, (DateTime?)null));
        }

        public async Task ReleaseReminderClaimAsync(int periodId)
        {
            await _context.TenantSubscriptionPeriods
                .Where(period => period.Id == periodId)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(period => period.ReminderClaimedUntil, (DateTime?)null));
        }

        public async Task<bool> InitializeSubscriptionAsync(int companyId, DateTime startDate, DateTime createdAt)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
            var headquarters = await _context.Tenants.FirstOrDefaultAsync(item =>
                item.CompanyId == companyId && item.IsHeadquarters);
            if (company is null
                || headquarters is null
                || await _context.TenantSubscriptionPeriods.AnyAsync(period => period.CompanyId == companyId))
                return false;

            startDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Unspecified);
            company.SubscriptionStartDate = startDate;
            headquarters.fechaInicioSuscripcion = startDate;

            _context.TenantSubscriptionPeriods.AddRange(
                Enumerable.Range(0, 12).Select(month => new TenantSubscriptionPeriod
                {
                    CompanyId = companyId,
                    TenantId = headquarters.Id,
                    CycleNumber = 1,
                    MonthNumber = month + 1,
                    DueDate = startDate.AddMonths(month),
                    CreatedAt = createdAt
                }));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> RecordPaymentAsync(int periodId, DateTime paidAt, string recordedBy, DateTime today)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var companyId = await _context.TenantSubscriptionPeriods
                .Where(item => item.Id == periodId)
                .Select(item => item.CompanyId)
                .FirstOrDefaultAsync();
            if (companyId == 0)
                return false;

            var affected = await _context.TenantSubscriptionPeriods
                .Where(item => item.Id == periodId && item.PaidAt == null)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.PaidAt, paidAt)
                    .SetProperty(item => item.PaymentRecordedBy, recordedBy));
            if (affected != 1)
                return false;

            var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
            if (company is null)
                return false;

            var overdueBefore = today.AddDays(-3);
            var hasOverdueBalance = await _context.TenantSubscriptionPeriods
                .AnyAsync(item => item.CompanyId == company.Id
                    && item.PaidAt == null
                    && item.DueDate < overdueBefore);

            if (company.SuspendedForOverdue && !hasOverdueBalance)
            {
                company.IsActive = true;
                company.SuspendedForOverdue = false;
                await _context.Tenants
                    .Where(branch => branch.CompanyId == company.Id)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(branch => branch.estado, true)
                        .SetProperty(branch => branch.bloqueadoPorMora, false));
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> DeactivateCompanyAsync(int companyId, DateTime overdueBefore)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
            if (company is null || !company.IsActive)
                return false;

            var hasOverdueBalance = await _context.TenantSubscriptionPeriods
                .AnyAsync(period => period.CompanyId == companyId
                    && period.PaidAt == null
                    && period.DueDate < overdueBefore);
            company.IsActive = false;
            company.SuspendedForOverdue = hasOverdueBalance;
            await _context.Tenants
                .Where(branch => branch.CompanyId == companyId)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(branch => branch.estado, false)
                    .SetProperty(branch => branch.bloqueadoPorMora, hasOverdueBalance));
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> ActivateCompanyAsync(int companyId)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
            if (company is null || company.IsActive)
                return false;

            company.IsActive = true;
            company.SuspendedForOverdue = false;
            await _context.Tenants
                .Where(branch => branch.CompanyId == companyId)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(branch => branch.estado, true)
                    .SetProperty(branch => branch.bloqueadoPorMora, false));
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RenewSubscriptionAsync(int companyId, DateTime createdAt)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId);
            var headquarters = await _context.Tenants.FirstOrDefaultAsync(item =>
                item.CompanyId == companyId && item.IsHeadquarters);
            if (company is null || headquarters is null)
                return false;

            var periods = await _context.TenantSubscriptionPeriods
                .Where(period => period.CompanyId == companyId)
                .OrderBy(period => period.CycleNumber)
                .ThenBy(period => period.MonthNumber)
                .ToListAsync();
            if (periods.Count == 0)
                return false;

            var latestCycleNumber = periods.Max(period => period.CycleNumber);
            var latestCycle = periods.Where(period => period.CycleNumber == latestCycleNumber).ToList();
            if (latestCycle.Count != 12
                || latestCycle.Any(period => period.PaidAt is null)
                || !latestCycle.Select(period => period.MonthNumber).Order().SequenceEqual(Enumerable.Range(1, 12)))
            {
                return false;
            }

            var nextCycleStart = latestCycle.Max(period => period.DueDate);
            _context.TenantSubscriptionPeriods.AddRange(
                Enumerable.Range(0, 12).Select(month => new TenantSubscriptionPeriod
                {
                    CompanyId = companyId,
                    TenantId = headquarters.Id,
                    CycleNumber = latestCycleNumber + 1,
                    MonthNumber = month + 1,
                    DueDate = nextCycleStart.AddMonths(month + 1),
                    CreatedAt = createdAt
                }));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
    }
}
