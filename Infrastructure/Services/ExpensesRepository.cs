using System.Data;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class ExpensesRepository : IExpensesRepository
    {
        private readonly TenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public ExpensesRepository(TenantProvider tenantProvider, AppDbContext context)
        {
            _tenantProvider = tenantProvider;
            _context = context;
        }

        public async Task AddAsync(Egreso egreso)
        {
            await _context.Egresos.AddAsync(egreso);
        }

        public async Task<List<ExpenseDto>> GetByTenantAsync(int tenantId)
        {
            return await _context.Egresos
            .Include(x => x.TipoEgreso)
            .Include(x => x.UsuarioRadica)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.Fecha)
            .Select(x => new ExpenseDto
            {
                Id = x.Id,
                TipoEgreso = x.TipoEgreso.Nombre,
                Valor = x.Valor,
                Descripcion = x.Descripcion,
                Fecha = x.Fecha,
                Referencia = x.Referencia,

                UsuarioRadicaId = x.CreadoPor ?? 0,

                UsuarioRadicaNombre = x.UsuarioRadica != null
                    ? x.UsuarioRadica.nombre + ' ' + x.UsuarioRadica.apellidos
                    : null
            })
            .ToListAsync();
        }

        public async Task<List<TipoEgreso>> GetAllExpensesTypesAsync()
        {
            return await _context.TipoEgresos
                .Where(x => x.Activo)
                .ToListAsync();
        }

        public async Task<TipoEgreso?> GetExpenseTypeByIdAsync(int id)
        {
            return await _context.TipoEgresos.FindAsync(id);
        }

    }
}
