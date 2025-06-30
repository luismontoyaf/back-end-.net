using Core.Models;
using Core.Interfaces;
using Newtonsoft.Json;

namespace Application.Services
{
    public class SaleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SaleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SaveSaleAsync(InvoiceRequest request)
        {
            // 1. Obtener el cliente por documento  
            var cliente = await _unitOfWork.Clientes.GetClientByDocumentAsync(request.idClient);
            if (cliente == null) throw new Exception("Cliente no encontrado");

            // 2. Calcular total  
            var total = request.Items.Sum(i => i.UnitPrice * i.Quantity);

            var productos = request.Items.Select(item => new
            {
                Nombre = item.ProductName,
                Cantidad = item.Quantity,
                ValorUnitario = item.UnitPrice,
                TotalProducto = item.Quantity * item.UnitPrice
            }).ToList();

            var jsonFactura = JsonConvert.SerializeObject(new
            {
                Total = total,
                Productos = productos
            }, Formatting.Indented);

            var sale = new Sale
            {
                IdCliente = cliente.Id,
                JsonFactura = jsonFactura,
                FormaPago = request.PaymentMethod
            };

            // 5. Guardar  
            await _unitOfWork.Ventas.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
