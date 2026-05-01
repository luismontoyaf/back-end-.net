using System.Text.Json;
using Core.Interfaces;
using Core.Models;
using Newtonsoft.Json;

namespace Application.Services
{
    public class SaleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductRepository _productRepository;
        private readonly TenantProvider _tenantProvider;

        public SaleService(IUnitOfWork unitOfWork, IProductRepository productRepository, TenantProvider tenantProvider)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
            _tenantProvider = tenantProvider;
        }

        public async Task<Sale> SaveSaleAsync(InvoiceRequest request)
        {
            try
            {
                var tenantId = _tenantProvider.GetTenantId();

                var cliente = await _unitOfWork.Clientes.GetClientByIdAsync(request.idClient, tenantId);
                if (cliente == null || cliente.TenantId != tenantId)
                    throw new Exception("Cliente no encontrado");

                foreach (var item in request.Items)
                {
                    var producto = _productRepository.GetProductById(item.Id ?? throw new Exception("Id no encontrado"));

                    if (producto == null || producto.TenantId != tenantId)
                        throw new Exception($"Producto no encontrado ({item.ProductName})");

                    if (producto.stock < item.Quantity)
                        throw new Exception($"Stock insuficiente para el producto {producto.nombreProducto}");

                    producto.stock -= item.Quantity;

                    _productRepository.Update(producto);
                }

                var total = request.Items.Sum(i => i.UnitPrice * i.Quantity);

                var productos = request.Items.Select(item => new
                {
                    Nombre = item.ProductName,
                    Cantidad = item.Quantity,
                    ValorUnitario = item.UnitPrice,
                    TotalProducto = item.Quantity * item.UnitPrice
                }).ToList();

                var facturaObject = new
                {
                    Total = total,
                    Productos = productos
                };

                var jsonFactura = JsonDocument.Parse(JsonConvert.SerializeObject(facturaObject));

                var sale = new Sale
                {
                    IdCliente = cliente.Id,
                    TenantId = tenantId,
                    JsonFactura = jsonFactura,
                    FormaPago = request.PaymentMethod
                };

                await _unitOfWork.Ventas.AddAsync(sale);
                await _unitOfWork.SaveChangesAsync();

                return sale;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar la venta: {ex.Message}");
            }
        }
    }
}
