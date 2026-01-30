using Core.Models;
using Core.Interfaces;
using Newtonsoft.Json;

namespace Application.Services
{
    public class SaleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductRepository _productRepository;

        public SaleService(IUnitOfWork unitOfWork, IProductRepository productRepository)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
        }

        public async Task<Sale> SaveSaleAsync(InvoiceRequest request)
        {
            // 1. Obtener el cliente por documento  
            var cliente = await _unitOfWork.Clientes.GetClientByIdAsync(request.idClient);
            if (cliente == null) throw new Exception("Cliente no encontrado");

            // 2. Validar stock y descontar
            foreach (var item in request.Items)
            {
                var producto = _productRepository.GetProductById(item.Id ?? throw new ("Id no encontrado"));

                if (producto == null)
                    throw new Exception($"Producto no encontrado (Nombre {item.ProductName})");

                if (producto.stock < item.Quantity)
                    throw new Exception($"Stock insuficiente para el producto {producto.nombreProducto}");

                producto.stock -= item.Quantity;

                _productRepository.Update(producto);
            }

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

            return sale;
        }
    }
}
