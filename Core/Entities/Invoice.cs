namespace Core.Models
{
    public class InvoiceRequest
    {
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public List<InvoiceItem> Items { get; set; }
    }

    public class InvoiceData
    {
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public List<InvoiceItem> Items { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class InvoiceItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
