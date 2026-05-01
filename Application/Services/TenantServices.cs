using System.Text.Json;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Application.Services
{
    public class TenantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductRepository _productRepository;
        private readonly TenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public TenantService(IUnitOfWork unitOfWork, IProductRepository productRepository, TenantProvider tenantProvider, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        public Tenant? GetByIdentifier(string identifier)
        {
            return _context.Tenants
                .FirstOrDefault(t => t.identificador == identifier);
        }
    }
}
