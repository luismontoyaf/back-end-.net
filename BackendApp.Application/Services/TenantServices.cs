using System.Text.Json;
using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Application.Services
{
    public class TenantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductRepository _productRepository;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public TenantService(IUnitOfWork unitOfWork, IProductRepository productRepository, ITenantProvider tenantProvider, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        public Tenant? GetByIdentifier(string identifier)
        {
            return _context.Tenants
                .Include(tenant => tenant.Company)
                .FirstOrDefault(t => t.identificador == identifier);
        }

        public Tenant? GetById(int tenantId)
        {
            return _context.Tenants
                .Include(tenant => tenant.Company)
                .FirstOrDefault(tenant => tenant.Id == tenantId);
        }
    }
}


