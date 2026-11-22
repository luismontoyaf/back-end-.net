using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public sealed class SubscriptionService
    {
        private readonly ISubscriptionRepository _repository;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ISubscriptionRepository repository,
            EmailService emailService,
            IConfiguration configuration,
            ILogger<SubscriptionService> logger)
        {
            _repository = repository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<SubscriptionCompanyDto>> GetAllCompaniesAsync()
        {
            var today = GetColombiaToday();
            var companies = await _repository.GetAllCompaniesAsync();
            var allPeriods = await _repository.GetAllPeriodsAsync();
            var periodsByTenant = allPeriods
                .GroupBy(period => period.CompanyId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var result = new List<SubscriptionCompanyDto>(companies.Count);

            foreach (var company in companies)
            {
                var periods = periodsByTenant.GetValueOrDefault(company.Id, []);
                result.Add(new SubscriptionCompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Nit = company.Nit,
                    Email = company.Email,
                    SubscriptionPlan = company.SubscriptionPlan,
                    IsActive = company.IsActive,
                    SuspendedForOverdue = company.SuspendedForOverdue,
                    StartDate = company.SubscriptionStartDate,
                    Branches = company.Branches
                        .OrderByDescending(branch => branch.IsHeadquarters)
                        .ThenBy(branch => branch.nombre)
                        .Select(branch => new CompanyBranchDto
                        {
                            Id = branch.Id,
                            Name = branch.nombre,
                            Identifier = branch.identificador,
                            Nit = branch.nit ?? string.Empty,
                            Email = branch.correo ?? string.Empty,
                            CompanyNit = company.Nit,
                            CompanyEmail = company.Email,
                            Phone = branch.celular ?? string.Empty,
                            Address = branch.direccion ?? string.Empty,
                            IsActive = branch.estado,
                            IsHeadquarters = branch.IsHeadquarters
                        }).ToList(),
                    Periods = periods.Select(period => new SubscriptionPeriodDto
                    {
                        Id = period.Id,
                        CycleNumber = period.CycleNumber,
                        MonthNumber = period.MonthNumber,
                        DueDate = period.DueDate,
                        PaidAt = period.PaidAt,
                        Status = GetStatus(period, today),
                        ReminderCount = period.ReminderCount,
                        LastReminderSentAt = period.LastReminderSentAt
                    }).ToList()
                });
            }

            return result;
        }

        public async Task<bool> InitializeSubscriptionAsync(int companyId, DateTime startDate)
        {
            if (startDate == default)
                throw new ArgumentException("La fecha de inicio es obligatoria.");

            return await _repository.InitializeSubscriptionAsync(
                companyId,
                DateTime.SpecifyKind(startDate.Date, DateTimeKind.Unspecified),
                DateTime.UtcNow);
        }

        public Task<bool> RecordPaymentAsync(int periodId, string recordedBy) =>
            _repository.RecordPaymentAsync(
                periodId,
                DateTime.UtcNow,
                recordedBy,
                GetColombiaToday());

        public Task<bool> DeactivateCompanyAsync(int companyId) =>
            _repository.DeactivateCompanyAsync(
                companyId,
                GetColombiaToday().AddDays(-3));

        public Task<bool> ActivateCompanyAsync(int companyId) =>
            _repository.ActivateCompanyAsync(companyId);

        public Task<bool> RenewSubscriptionAsync(int companyId) =>
            _repository.RenewSubscriptionAsync(companyId, DateTime.UtcNow);

        public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
        {
            var creatorEmail = _configuration["PlatformCreator:Email"] ?? "canaldeatencionlumocol@gmail.com";
            var now = DateTime.UtcNow;
            var today = GetColombiaToday();
            var reminderCutoff = now.AddDays(-2);
            var candidates = await _repository.GetDueReminderCandidatesAsync(today, now, reminderCutoff);

            foreach (var (period, company) in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await _repository.TryClaimReminderAsync(period.Id, today, now, reminderCutoff))
                    continue;

                try
                {
                    var senderEmail = _configuration["MAILJET_SENDER_EMAIL"] ?? company.Email;
                    if (string.IsNullOrWhiteSpace(company.Email))
                        throw new InvalidOperationException($"La empresa {company.Name} no tiene correo de contacto.");
                    if (string.IsNullOrWhiteSpace(senderEmail))
                        throw new InvalidOperationException("No hay un correo remitente configurado para los recordatorios.");

                    var reminderNumber = period.ReminderCount + 1;
                    var recipients = new[] { company.Email, creatorEmail }
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var deliveredRecipients = await _repository.GetDeliveredReminderRecipientsAsync(
                        period.Id,
                        reminderNumber);

                    foreach (var recipient in recipients.Where(email => !deliveredRecipients.Contains(email)))
                    {
                        await _emailService.SendSubscriptionOverdueReminderAsync(
                            recipient,
                            string.Equals(recipient, company.Email, StringComparison.OrdinalIgnoreCase)
                                ? company.Name
                                : "Equipo Lumo",
                            company.Name,
                            senderEmail,
                            period.DueDate);
                        await _repository.RecordReminderDeliveryAsync(
                            period.Id,
                            reminderNumber,
                            recipient,
                            DateTime.UtcNow);
                    }

                    await _repository.CompleteReminderAsync(period.Id, DateTime.UtcNow);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Could not send subscription reminder for tenant {TenantId}, period {PeriodId}.",
                        company.Id,
                        period.Id);
                    await _repository.ReleaseReminderClaimAsync(period.Id);
                }
            }
        }

        private static string GetStatus(TenantSubscriptionPeriod period, DateTime today)
        {
            if (period.PaidAt is not null)
                return "Pagado";

            return period.DueDate < today.AddDays(-3) ? "En mora" : "Pendiente";
        }

        private static DateTime GetColombiaToday() =>
            DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-5).Date, DateTimeKind.Unspecified);
    }
}
