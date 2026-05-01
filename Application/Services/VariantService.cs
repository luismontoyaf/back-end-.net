using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Newtonsoft.Json;

namespace Application.Services
{
    public class VariantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVariantRepository _IvariantRepository;
        private readonly TenantProvider _tenantProvider;

        public VariantService(IVariantRepository IvariantRepository, IUnitOfWork unitOfWork, TenantProvider tenantProvider)
        {
            _unitOfWork = unitOfWork;
            _IvariantRepository = IvariantRepository;
            _tenantProvider = tenantProvider;
        }

        public async Task<IEnumerable<Variant>> GetVariants()
        {
            var tenantId = _tenantProvider.GetTenantId();

            var variants = await _IvariantRepository.GetVariants();

            return (variants);
        }

        public async Task SaveVariantAsync(VariantDto variantDto)
        {
            if (string.IsNullOrEmpty(variantDto.Name))
                throw new ArgumentException("Datos inválidos");

            var tenantId = _tenantProvider.GetTenantId();

            var variant = new Variant
            {
                Name = variantDto.Name,
                JsonValues = JsonConvert.SerializeObject(variantDto.Atributes),
                State = true,
                TenantId = tenantId
            };

            await _unitOfWork.Variants.SaveVariantAsync(variant);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task changeStatusVariant(int id)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var variant = await _IvariantRepository.GetVariantById(id);

            if (variant == null || variant.TenantId != tenantId)
                throw new Exception("No se encontró la variante");

            variant.State = !variant.State;

            _IvariantRepository.Update(variant);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateVariantAsync(int id, VariantDto dto)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var variant = await _IvariantRepository.GetVariantById(id);

            if (variant == null || variant.TenantId != tenantId)
                throw new Exception("Variante no encontrada");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                variant.Name = dto.Name;

            if (dto.Atributes != null && dto.Atributes.Any())
                variant.JsonValues = JsonConvert.SerializeObject(dto.Atributes);

            _IvariantRepository.Update(variant);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteVariant(int id)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var variant = await _IvariantRepository.GetVariantById(id);

            if (variant == null || variant.TenantId != tenantId)
                throw new Exception("No se encontró la variante");

            _IvariantRepository.Delete(variant);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
