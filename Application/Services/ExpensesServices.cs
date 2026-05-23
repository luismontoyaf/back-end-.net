using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;

namespace Application.Services
{
    public class ExpensesService
    {
        private readonly IExpensesRepository _expensesRepository;
        private readonly TenantProvider _tenantProvider;
        private readonly IUnitOfWork _unitOfWork;


        public ExpensesService(IExpensesRepository expensesRepository, TenantProvider tenantProvider, IUnitOfWork unitOfWork)
        {
            _tenantProvider = tenantProvider;
            _unitOfWork = unitOfWork;
            _expensesRepository = expensesRepository;
        }

        public async Task CreateAsync(CreateExpenseDto dto)
        {
            var egreso = new Egreso
            {
                TipoEgresoId = dto.TipoEgresoId,
                Valor = dto.Valor,
                Descripcion = dto.Descripcion,
                Referencia = dto.Referencia,
                TenantId = _tenantProvider.GetTenantId(),
                Fecha = DateTime.UtcNow,
                CreadoPor = _tenantProvider.GetUserId() 
            };

            await _expensesRepository.AddAsync(egreso);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<ExpenseDto>> GetByTenantAsync()
        {
            var tenantId = _tenantProvider.GetTenantId();

            return await _expensesRepository.GetByTenantAsync(tenantId);
        }

        public async Task<List<TipoEgreso>> GetAllExpensesTypesAsync()
        {
            return await _expensesRepository.GetAllExpensesTypesAsync();
        }
    }
}
