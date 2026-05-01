using System.Collections.Generic;
using System.Data;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Infrastructure.Services
{
    public class VariantRepository : IVariantRepository
    {
        private readonly AppDbContext _context;
        private readonly TenantProvider _tenantProvider;

        public VariantRepository(AppDbContext context, TenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task SaveVariantAsync(Variant variant)
        {
            var tenantId = _tenantProvider.GetTenantId();
            variant.TenantId = tenantId;
            await _context.Variants.AddAsync(variant);
        }

        async Task<List<Variant>> IVariantRepository.GetVariants()
        {
            var tenantId = _tenantProvider.GetTenantId();

            return await _context.Variants
                .Where(u => u.TenantId == tenantId)
                .Select(u => new Variant
                {
                    Id = u.Id,
                    Name = u.Name,
                    JsonValues = u.JsonValues,
                    State = u.State
                })
                .ToListAsync();
        }

        public async Task<Variant?> GetVariantById(int id)
        {
            var tenantId = _tenantProvider.GetTenantId();

            return await _context.Variants
                .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId);
        }

        void IVariantRepository.Update(Variant variant)
        {
            _context.Variants.Update(variant);
        }

        void IVariantRepository.Delete(Variant variant)
        {
            _context.Variants.Remove(variant);
        }
    }
}
