using Core.Models;

namespace Core.Interfaces
{
    public interface IExpensesRepository
    {
        Task AddAsync(Egreso egreso);
        Task<List<ExpenseDto>> GetByTenantAsync(int tenantId);

        Task<List<TipoEgreso>> GetAllExpensesTypesAsync();
        Task<TipoEgreso?> GetExpenseTypeByIdAsync(int id);
    }
}