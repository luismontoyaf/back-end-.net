using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Core.Models;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace Infrastructure.Services
{
    public class VariantRepository : IVariantRepository
    {
        private readonly AppDbContext _context;

        public VariantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveVariantAsync(Variant variant) => await _context.Variants.AddAsync(variant);

        Task<List<Variant>> IVariantRepository.GetVariants()
        {
            return _context.Variants.Select(u => new Variant
            {
                Id = u.Id,
                Name = u.Name,
                JsonValues = u.JsonValues,
                State = u.State
            }).ToListAsync();
        }

        public async Task<Variant?> GetVariantById(int id)
        {
            return await _context.Variants.FindAsync(id);
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
