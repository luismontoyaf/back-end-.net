using Core.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Core.Interfaces
{
    public interface IVariantRepository
    {
        Task SaveVariantAsync(Variant variant);

        Task <List<Variant>>GetVariants();

        Task<Variant?> GetVariantById(int id);
        void Update(Variant variant);
        void Delete(Variant variant);

    }
}