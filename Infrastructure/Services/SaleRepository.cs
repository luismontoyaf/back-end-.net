using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Core.Models;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Services
{
    public class SaleRepository : ISaleRepository
    {
        private readonly AppDbContext _context;
        public SaleRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(Sale sale) => await _context.Ventas.AddAsync(sale);

        public async Task<int> GetLastInvoiceNumber()
        {
            return await _context.Ventas.OrderByDescending(v => v.IdFactura).Select(v => v.IdFactura).FirstOrDefaultAsync();
        }

        public async Task<Sale> GetInvoiceByClientId(int idCliente)
        {
            var sale = await _context.Ventas
            .Where(v => v.IdCliente == idCliente)
            .OrderByDescending(v => v.FechaCreacion)
            .FirstOrDefaultAsync();

            return sale ?? throw new InvalidOperationException("No se encontró una factura para el cliente especificado.");
        }

    }
}
