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

        public VariantService(IVariantRepository IvariantRepository, IUnitOfWork unitOfWork )
        {
            _unitOfWork = unitOfWork;
            _IvariantRepository = IvariantRepository;
        }

        public async Task SaveVariantAsync(VariantDto variantDto)
        {
            if (string.IsNullOrEmpty(variantDto.Name))
            {
                throw new ArgumentException("Datos inválidos");
            }

            var variant = new Variant
            {
                Name = variantDto.Name,
                JsonValues = JsonConvert.SerializeObject(variantDto.Atributes),
                State = true
            };

            await _unitOfWork.Variants.SaveVariantAsync(variant);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task changeStatusVariant(int id)
        {
            var variant = await _IvariantRepository.GetVariantById(id);

            if (variant == null) throw new Exception("No se encontró la variante");

            variant.State = variant.State ? false : true;

            _IvariantRepository.Update(variant);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateVariantAsync(int id, VariantDto dto)
        {
            var variant = await _IvariantRepository.GetVariantById(id);

            if (variant == null)
                throw new Exception("Variante no encontrada");

            // PATCH ? solo lo que venga
            if (!string.IsNullOrWhiteSpace(dto.Name))
                variant.Name = dto.Name;

            if (dto.Atributes != null && dto.Atributes.Any())
                variant.JsonValues = JsonConvert.SerializeObject(dto.Atributes);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteVariant(int id)
        {
            var variant = await _IvariantRepository.GetVariantById(id);

            if (variant == null) throw new Exception("No se encontró la variante");

            _IvariantRepository.Delete(variant);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
