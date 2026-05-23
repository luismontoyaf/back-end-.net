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

                Client cliente;

                if (request.ClientDocument == "222222222222")
                {
                    cliente = await _unitOfWork.Clientes.GetFinalCustomer();
                }else
                {
                    cliente = await _unitOfWork.Clientes.GetClientByIdAsync(request.idClient, tenantId);
                }

                if ((cliente == null || cliente.TenantId != tenantId) && request.ClientDocument != "222222222222")
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

                var discount = request.DiscountPercentage;

                var subtotal = request.Items.Sum(i => i.UnitPrice * i.Quantity);

                var discountAmount = subtotal * (discount / 100m);
                var total = subtotal - discountAmount;

                var productos = request.Items.Select(item =>
                {
                    var lineSubtotal = item.UnitPrice * item.Quantity;
                    var lineDiscount = lineSubtotal * (discount / 100m);

                    return new
                    {
                        Nombre = item.ProductName,
                        Cantidad = item.Quantity,
                        ValorUnitario = item.UnitPrice,
                        Subtotal = lineSubtotal
                    };
                }).ToList();

                var facturaObject = new
                {
                    Subtotal = subtotal,
                    DescuentoPorcentaje = discount,
                    DescuentoTotal = discountAmount,
                    Total = total,
                    Productos = productos
                };

                var jsonFactura = JsonDocument.Parse(JsonConvert.SerializeObject(facturaObject));

                var sale = new Sale
                {
                    IdCliente = cliente.Id,
                    TenantId = tenantId,
                    JsonFactura = jsonFactura,
                    FormaPago = request.PaymentMethod,
                    IdVendedor = _tenantProvider.GetUserId()
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
