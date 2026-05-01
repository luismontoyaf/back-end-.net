using System.Collections.Generic;
using System.Data;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly TenantProvider _tenantProvider;

        public InvoiceRepository(string connectionString, AppDbContext context, TenantProvider tenantProvider)
        {
            _connectionString = connectionString;
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public Client GetUserInfoByDocument(string cedula)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var client = _context.Clientes
                .Where(u => u.numDocumento == cedula && u.TenantId == tenantId) // 🔥 CLAVE
                .Select(u => new Client
                {
                    Id = u.Id,
                    nombre = u.nombre,
                    apellidos = u.apellidos,
                    tipoDocumento = u.tipoDocumento,
                    numDocumento = u.numDocumento,
                    fechaNacimiento = (DateTime)u.fechaNacimiento,
                    contrasena = u.contrasena,
                    direccion = u.direccion,
                    correo = u.correo,
                    celular = u.celular
                })
                .FirstOrDefault();

            return client;
        }

        public async Task<List<Sale>> GetAllInvoices()
        {
            var tenantId = _tenantProvider.GetTenantId();

            return await _context.Ventas
                .Where(v => v.TenantId == tenantId)
                .ToListAsync();
        }

    }
}
