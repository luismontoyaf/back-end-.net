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
    public class SaleRepository : ISaleRepository
    {
        private readonly AppDbContext _context;
        private readonly TenantProvider _tenantProvider;

        public SaleRepository(AppDbContext context, TenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task AddAsync(Sale sale)
        {
            await _context.Ventas.AddAsync(sale);
        }

        public async Task<int> GetLastInvoiceNumber()
        {
            var tenantId = _tenantProvider.GetTenantId();

            return await _context.Ventas
                .Where(v => v.TenantId == tenantId)
                .OrderByDescending(v => v.IdFactura)
                .Select(v => v.IdFactura)
                .FirstOrDefaultAsync();
        }

        public async Task<Sale> GetInvoiceByClientId(int idCliente)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var sale = await _context.Ventas
                .Where(v => v.IdCliente == idCliente && v.TenantId == tenantId)
                .OrderByDescending(v => v.FechaCreacion)
                .FirstOrDefaultAsync();

            return sale ?? throw new InvalidOperationException("No se encontró una factura para el cliente especificado.");
        }

        public async Task<Sale> GetInvoiceByInvoiceNumber(string numFactura, int tenantId)
        {
            var sale = await _context.Ventas
                .Where(v => v.NumeroFactura == numFactura && v.TenantId == tenantId)
                .FirstOrDefaultAsync();

            return sale ?? throw new InvalidOperationException("No se encontró una factura para el cliente especificado.");
        }
    }
}
