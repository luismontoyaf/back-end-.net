using System.Net.Mail;
using System.Text.RegularExpressions;
using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class CompanySettingsService
    {
        private readonly AppDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public CompanySettingsService(AppDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<CompanySettingsDto> GetCurrentCompanyAsync()
        {
            var company = await GetCompanyForCurrentTenantAsync();
            var latestUpgradeRequest = await _context.CompanyPlanUpgradeRequests
                .AsNoTracking()
                .Where(request => request.CompanyId == company.Id)
                .OrderByDescending(request => request.RequestedAt)
                .FirstOrDefaultAsync();

            return ToSettingsDto(company, latestUpgradeRequest);
        }

        public async Task<CompanySettingsDto> UpdateCurrentCompanyAsync(UpdateCompanySettingsRequest request)
        {
            ValidateCompanySettings(request);
            var company = await GetCompanyForCurrentTenantAsync();

            company.Name = request.Name.Trim();
            company.Nit = request.Nit.Trim();
            company.Email = request.Email.Trim();
            company.Phone = request.Phone.Trim();
            company.Address = request.Address.Trim();
            await _context.SaveChangesAsync();

            var latestUpgradeRequest = await _context.CompanyPlanUpgradeRequests
                .AsNoTracking()
                .Where(item => item.CompanyId == company.Id)
                .OrderByDescending(item => item.RequestedAt)
                .FirstOrDefaultAsync();
            return ToSettingsDto(company, latestUpgradeRequest);
        }

        public async Task<CompanySettingsDto> RequestPlusUpgradeAsync(string requestedByEmail)
        {
            var company = await GetCompanyForCurrentTenantAsync();
            if (string.Equals(company.SubscriptionPlan, CompanyPlan.Plus, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La empresa ya tiene el plan Plus.");

            var hasPendingRequest = await _context.CompanyPlanUpgradeRequests.AnyAsync(request =>
                request.CompanyId == company.Id && request.Status == PlanUpgradeRequestStatus.Pending);
            if (hasPendingRequest)
                throw new InvalidOperationException("Ya existe una solicitud de mejora pendiente para esta empresa.");

            var upgradeRequest = new CompanyPlanUpgradeRequest
            {
                CompanyId = company.Id,
                RequestedByEmail = requestedByEmail,
                RequestedAt = DateTime.UtcNow,
                Status = PlanUpgradeRequestStatus.Pending
            };
            _context.CompanyPlanUpgradeRequests.Add(upgradeRequest);
            await _context.SaveChangesAsync();

            return ToSettingsDto(company, upgradeRequest);
        }

        public async Task<List<PlanUpgradeRequestDto>> GetPendingUpgradeRequestsAsync()
        {
            return await _context.CompanyPlanUpgradeRequests
                .AsNoTracking()
                .Include(request => request.Company)
                .Where(request => request.Status == PlanUpgradeRequestStatus.Pending)
                .OrderBy(request => request.RequestedAt)
                .Select(request => new PlanUpgradeRequestDto
                {
                    Id = request.Id,
                    CompanyId = request.CompanyId,
                    CompanyName = request.Company.Name,
                    CompanyNit = request.Company.Nit,
                    RequestedByEmail = request.RequestedByEmail,
                    RequestedAt = request.RequestedAt
                })
                .ToListAsync();
        }

        public async Task<bool> ApprovePlusUpgradeAsync(int requestId, string approvedByEmail)
        {
            var request = await _context.CompanyPlanUpgradeRequests
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item =>
                    item.Id == requestId && item.Status == PlanUpgradeRequestStatus.Pending);
            if (request is null)
                return false;

            request.Company.SubscriptionPlan = CompanyPlan.Plus;
            request.Status = PlanUpgradeRequestStatus.Approved;
            request.DecidedAt = DateTime.UtcNow;
            request.DecidedByEmail = approvedByEmail;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectPlusUpgradeAsync(int requestId, string rejectedByEmail)
        {
            var request = await _context.CompanyPlanUpgradeRequests
                .FirstOrDefaultAsync(item =>
                    item.Id == requestId && item.Status == PlanUpgradeRequestStatus.Pending);
            if (request is null)
                return false;

            request.Status = PlanUpgradeRequestStatus.Rejected;
            request.DecidedAt = DateTime.UtcNow;
            request.DecidedByEmail = rejectedByEmail;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<Company> GetCompanyForCurrentTenantAsync()
        {
            var tenantId = _tenantProvider.GetTenantId();
            var tenant = await _context.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == tenantId);
            if (tenant is null)
                throw new InvalidOperationException("La sede activa no existe.");

            var company = await _context.Companies
                .FirstOrDefaultAsync(item => item.Id == tenant.CompanyId);
            if (company is null)
                throw new InvalidOperationException("No se encontró la empresa de la sede activa.");

            return company;
        }

        private static void ValidateCompanySettings(UpdateCompanySettingsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)
                || string.IsNullOrWhiteSpace(request.Nit)
                || string.IsNullOrWhiteSpace(request.Email)
                || !IsValidEmail(request.Email)
                || string.IsNullOrWhiteSpace(request.Phone)
                || !Regex.IsMatch(request.Phone.Trim(), "^[0-9]{7,10}$")
                || string.IsNullOrWhiteSpace(request.Address))
            {
                throw new ArgumentException("Completa los datos de la empresa y verifica que el teléfono tenga entre 7 y 10 dígitos.");
            }
        }

        private static bool IsValidEmail(string email) =>
            MailAddress.TryCreate(email.Trim(), out var address)
            && string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);

        private static CompanySettingsDto ToSettingsDto(
            Company company,
            CompanyPlanUpgradeRequest? upgradeRequest) => new()
        {
            Id = company.Id,
            Name = company.Name,
            Nit = company.Nit,
            Email = company.Email,
            Phone = company.Phone,
            Address = company.Address,
            SubscriptionPlan = company.SubscriptionPlan,
            UpgradeRequestStatus = upgradeRequest?.Status,
            UpgradeRequestedAt = upgradeRequest?.RequestedAt
        };
    }
}
