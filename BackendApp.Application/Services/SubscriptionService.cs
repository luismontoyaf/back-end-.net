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

        public async Task<List<SubscriptionTenantDto>> GetAllTenantsAsync()
        {
            var today = GetColombiaToday();
            var tenants = await _repository.GetAllTenantsAsync();
            var allPeriods = await _repository.GetAllPeriodsAsync();
            var periodsByTenant = allPeriods
                .GroupBy(period => period.TenantId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var result = new List<SubscriptionTenantDto>(tenants.Count);

            foreach (var tenant in tenants)
            {
                var periods = periodsByTenant.GetValueOrDefault(tenant.Id, []);
                result.Add(new SubscriptionTenantDto
                {
                    Id = tenant.Id,
                    Name = tenant.nombre,
                    Identifier = tenant.identificador,
                    Nit = tenant.nit ?? string.Empty,
                    Email = tenant.correo ?? string.Empty,
                    IsActive = tenant.estado,
                    SuspendedForOverdue = tenant.bloqueadoPorMora,
                    StartDate = tenant.fechaInicioSuscripcion,
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

        public async Task<bool> InitializeSubscriptionAsync(int tenantId, DateTime startDate)
        {
            if (startDate == default)
                throw new ArgumentException("La fecha de inicio es obligatoria.");

            return await _repository.InitializeSubscriptionAsync(
                tenantId,
                DateTime.SpecifyKind(startDate.Date, DateTimeKind.Unspecified),
                DateTime.UtcNow);
        }

        public Task<bool> RecordPaymentAsync(int periodId, string recordedBy) =>
            _repository.RecordPaymentAsync(
                periodId,
                DateTime.UtcNow,
                recordedBy,
                GetColombiaToday());

        public Task<bool> DeactivateTenantAsync(int tenantId) =>
            _repository.DeactivateTenantAsync(
                tenantId,
                GetColombiaToday().AddDays(-3));

        public Task<bool> ActivateTenantAsync(int tenantId) =>
            _repository.ActivateTenantAsync(tenantId);

        public Task<bool> RenewSubscriptionAsync(int tenantId) =>
            _repository.RenewSubscriptionAsync(tenantId, DateTime.UtcNow);

        public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
        {
            var creatorEmail = _configuration["PlatformCreator:Email"] ?? "canaldeatencionlumocol@gmail.com";
            var now = DateTime.UtcNow;
            var today = GetColombiaToday();
            var reminderCutoff = now.AddDays(-2);
            var candidates = await _repository.GetDueReminderCandidatesAsync(today, now, reminderCutoff);

            foreach (var (period, tenant) in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await _repository.TryClaimReminderAsync(period.Id, today, now, reminderCutoff))
                    continue;

                try
                {
                    var senderEmail = _configuration["MAILJET_SENDER_EMAIL"] ?? tenant.correo;
                    if (string.IsNullOrWhiteSpace(tenant.correo))
                        throw new InvalidOperationException($"El tenant {tenant.identificador} no tiene correo de contacto.");
                    if (string.IsNullOrWhiteSpace(senderEmail))
                        throw new InvalidOperationException("No hay un correo remitente configurado para los recordatorios.");

                    var reminderNumber = period.ReminderCount + 1;
                    var recipients = new[] { tenant.correo, creatorEmail }
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var deliveredRecipients = await _repository.GetDeliveredReminderRecipientsAsync(
                        period.Id,
                        reminderNumber);

                    foreach (var recipient in recipients.Where(email => !deliveredRecipients.Contains(email)))
                    {
                        await _emailService.SendSubscriptionOverdueReminderAsync(
                            recipient,
                            string.Equals(recipient, tenant.correo, StringComparison.OrdinalIgnoreCase)
                                ? tenant.nombre
                                : "Equipo Lumo",
                            tenant.nombre,
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
                        tenant.Id,
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
