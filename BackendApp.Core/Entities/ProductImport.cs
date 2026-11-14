namespace Core.Models
{
    public class ProductImportRow
    {
        public string? CodigoProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? Descripcion { get; set; }
        public int? Stock { get; set; }
        public decimal? Precio { get; set; }
        public bool? Activo { get; set; }
    }

    public class ProductImportError
    {
        public int Fila { get; set; }
        public string Campo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ProductImportValidationResult
    {
        public bool Valido => Errores.Count == 0 && TotalFilas > 0;
        public int TotalFilas { get; set; }
        public List<ProductImportError> Errores { get; set; } = new();
    }
}
