using System.Text.RegularExpressions;
using System.Net.Mail;
using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class BranchManagementService
    {
        private readonly AppDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public BranchManagementService(AppDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<CompanyBranchDto>> GetBranchesAsync()
        {
            var currentBranch = await GetCurrentBranchAsync();
            return await _context.Tenants.AsNoTracking()
                .Include(branch => branch.Company)
                .Where(branch => branch.CompanyId == currentBranch.CompanyId)
                .OrderByDescending(branch => branch.IsHeadquarters)
                .ThenBy(branch => branch.nombre)
                .Select(branch => new CompanyBranchDto
                {
                    Id = branch.Id,
                    Name = branch.nombre,
                    Identifier = branch.identificador,
                    Nit = branch.nit ?? string.Empty,
                    Email = branch.correo ?? string.Empty,
                    CompanyNit = branch.Company == null ? string.Empty : branch.Company.Nit,
                    CompanyEmail = branch.Company == null ? string.Empty : branch.Company.Email,
                    Phone = branch.celular ?? string.Empty,
                    Address = branch.direccion ?? string.Empty,
                    IsActive = branch.estado,
                    IsHeadquarters = branch.IsHeadquarters
                })
                .ToListAsync();
        }

        public async Task<CompanyBranchDto> CreateBranchAsync(CreateBranchRequest request)
        {
            ValidateBranch(request.Name, request.Identifier, request.Email);
            var currentBranch = await GetCurrentBranchAsync();
            var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == currentBranch.CompanyId);
            if (company is null || !company.IsActive)
                throw new InvalidOperationException("La empresa no está activa.");
            EnsurePlusPlan(company);

            var identifier = request.Identifier.Trim().ToLowerInvariant();
            if (await _context.Tenants.AnyAsync(branch => branch.identificador == identifier))
                throw new InvalidOperationException("Ya existe una sede con ese identificador.");

            var branch = new Tenant
            {
                CompanyId = company.Id,
                IsHeadquarters = false,
                nombre = request.Name.Trim(),
                identificador = identifier,
                nit = EmptyToNull(request.Nit),
                direccion = EmptyToNull(request.Address),
                celular = EmptyToNull(request.Phone),
                correo = EmptyToNull(request.Email),
                estado = true,
                fechaCreacion = DateTime.UtcNow,
                bloqueadoPorMora = false,
                Company = company
            };

            _context.Tenants.Add(branch);
            await _context.SaveChangesAsync();
            return ToBranchDto(branch);
        }

        public async Task<CompanyBranchDto?> UpdateBranchAsync(int branchId, CreateBranchRequest request)
        {
            ValidateBranch(request.Name, request.Identifier, request.Email);
            var currentBranch = await GetCurrentBranchAsync();
            var branch = await _context.Tenants.Include(item => item.Company).FirstOrDefaultAsync(item =>
                item.Id == branchId && item.CompanyId == currentBranch.CompanyId);
            if (branch is null)
                return null;

            var identifier = request.Identifier.Trim().ToLowerInvariant();
            if (await _context.Tenants.AnyAsync(item =>
                    item.identificador == identifier && item.Id != branch.Id))
                throw new InvalidOperationException("Ya existe una sede con ese identificador.");

            branch.nombre = request.Name.Trim();
            branch.identificador = identifier;
            branch.nit = EmptyToNull(request.Nit);
            branch.direccion = EmptyToNull(request.Address);
            branch.celular = EmptyToNull(request.Phone);
            branch.correo = EmptyToNull(request.Email);
            await _context.SaveChangesAsync();
            return ToBranchDto(branch);
        }

        public async Task<CompanyBranchDto> CreateCompanyAsync(CreateCompanyRequest request)
        {
            ValidateBranch(request.BranchName, request.BranchIdentifier, request.BranchEmail);
            if (!CompanyPlan.IsValid(request.SubscriptionPlan))
                throw new ArgumentException("Selecciona un plan válido para la empresa.");

            if (string.IsNullOrWhiteSpace(request.Name)
                || string.IsNullOrWhiteSpace(request.Email)
                || !IsValidEmail(request.Email)
                || string.IsNullOrWhiteSpace(request.AdminFirstName)
                || string.IsNullOrWhiteSpace(request.AdminLastName)
                || string.IsNullOrWhiteSpace(request.AdminEmail)
                || !IsValidEmail(request.AdminEmail)
                || string.IsNullOrWhiteSpace(request.AdminDocumentType)
                || string.IsNullOrWhiteSpace(request.AdminDocumentNumber)
                || string.IsNullOrWhiteSpace(request.AdminPhone)
                || string.IsNullOrWhiteSpace(request.AdminPassword)
                || request.AdminPassword.Trim().Length < 8
                || request.StartDate == default)
            {
                throw new ArgumentException("Completa los datos de la empresa, la sede, el administrador y el inicio de suscripción.");
            }

            var identifier = request.BranchIdentifier.Trim().ToLowerInvariant();
            if (await _context.Tenants.AnyAsync(branch => branch.identificador == identifier))
                throw new InvalidOperationException("Ya existe una sede con ese identificador.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var company = new Company
            {
                Name = request.Name.Trim(),
                Nit = request.Nit.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                Address = request.Address.Trim(),
                SubscriptionPlan = request.SubscriptionPlan.Trim().ToLowerInvariant(),
                IsActive = true,
                SubscriptionStartDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Unspecified),
                CreatedAt = DateTime.UtcNow
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var branch = new Tenant
            {
                CompanyId = company.Id,
                IsHeadquarters = true,
                nombre = request.BranchName.Trim(),
                identificador = identifier,
                nit = EmptyToNull(request.BranchNit) ?? EmptyToNull(request.Nit),
                direccion = EmptyToNull(request.BranchAddress),
                celular = EmptyToNull(request.BranchPhone),
                correo = EmptyToNull(request.BranchEmail) ?? request.Email.Trim(),
                estado = true,
                fechaCreacion = DateTime.UtcNow,
                fechaInicioSuscripcion = company.SubscriptionStartDate,
                bloqueadoPorMora = false,
                Company = company
            };
            _context.Tenants.Add(branch);
            await _context.SaveChangesAsync();

            _context.Usuarios.Add(new Employe
            {
                nombre = request.AdminFirstName.Trim(),
                apellidos = request.AdminLastName.Trim(),
                tipoDocumento = request.AdminDocumentType.Trim(),
                numDocumento = request.AdminDocumentNumber.Trim(),
                correo = request.AdminEmail.Trim(),
                fechaIngreso = DateTime.UtcNow,
                rol = 1,
                estado = 1,
                contrasena = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword.Trim(), workFactor: 12),
                celular = request.AdminPhone.Trim(),
                direccion = branch.direccion ?? string.Empty,
                TenantId = branch.Id
            });

            _context.TenantSubscriptionPeriods.AddRange(
                Enumerable.Range(0, 12).Select(month => new TenantSubscriptionPeriod
                {
                    CompanyId = company.Id,
                    TenantId = branch.Id,
                    CycleNumber = 1,
                    MonthNumber = month + 1,
                    DueDate = company.SubscriptionStartDate.Value.AddMonths(month),
                    CreatedAt = DateTime.UtcNow
                }));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ToBranchDto(branch);
        }

        private async Task<Tenant> GetCurrentBranchAsync()
        {
            var tenantId = _tenantProvider.GetTenantId();
            var branch = await _context.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId);
            if (branch is null)
                throw new InvalidOperationException("La sede activa no existe.");
            return branch;
        }

        private static void ValidateBranch(string name, string identifier, string? email = null)
        {
            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(identifier)
                || !Regex.IsMatch(identifier.Trim(), "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException("El nombre es obligatorio y el identificador solo puede contener letras, números y guiones.");
            }
            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                throw new ArgumentException("El correo de la sede no es válido.");
        }

        private static bool IsValidEmail(string email) =>
            MailAddress.TryCreate(email.Trim(), out var address)
            && string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);

        private static string? EmptyToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void EnsurePlusPlan(Company company)
        {
            if (!string.Equals(company.SubscriptionPlan, CompanyPlan.Plus, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El plan Standard permite una sola sede. Cambia al plan Plus para agregar o administrar sedes.");
        }

        private static CompanyBranchDto ToBranchDto(Tenant branch) => new()
        {
            Id = branch.Id,
            Name = branch.nombre,
            Identifier = branch.identificador,
            Nit = branch.nit ?? string.Empty,
            Email = branch.correo ?? string.Empty,
            CompanyNit = branch.Company?.Nit ?? string.Empty,
            CompanyEmail = branch.Company?.Email ?? string.Empty,
            Phone = branch.celular ?? string.Empty,
            Address = branch.direccion ?? string.Empty,
            IsActive = branch.estado,
            IsHeadquarters = branch.IsHeadquarters
        };
    }
}
