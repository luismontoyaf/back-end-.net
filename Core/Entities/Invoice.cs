namespace Core.Models
{
    public class InvoiceRequest
    {
        public string? ClientDocument { get; set; }
        public int idClient { get; set; }
        public string numInvoice { get; set; }
        public string PaymentMethod { get; set; }
        public List<InvoiceItem> Items { get; set; }
    }

    public class InvoiceData
    {
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public string ClientPhone { get; set; }
        public string ClientTypeDocument { get; set; }
        public string ClientDocument { get; set; }
        public string PaymentMethod { get; set; }
        public List<InvoiceItem> Items { get; set; }

        public string? InvoiceNumber { get; set; }
        public decimal TotalIva { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class InvoiceItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice;
    }

    public class DatosEmpresaWrapper
    {
        public DatosEmpresa DatosEmpresa { get; set; }
    }

    public class DatosEmpresa
    {
        public string Nombre { get; set; }
        public string Nit { get; set; }
        public string Direccion { get; set; }
        public string Correo { get; set; }
        public string Celular { get; set; }
    }
}
