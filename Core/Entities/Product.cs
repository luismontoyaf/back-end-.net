using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Product
    {
        [Key]
        public required int Id { get; set; }
        public int TenantId { get; set; }
        public required string nombreProducto { get; set; }
        public required string descripcion { get; set; }
        public required int stock { get; set; }
        public required decimal precio { get; set; }
        [NotMapped]
        public string? ImagenBase64 { get; set; } // Para enviar al frontend
        [NotMapped]
        public IFormFile? ImagenFile { get; set; } // Para recibir archivos al agregar
        public bool? activo { get; set; } = true;

        public ImagenProducto? Imagen { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string nombreProducto { get; set; } = null!;
        public string descripcion { get; set; } = null!;
        public int stock { get; set; }
        public decimal precio { get; set; }
        public bool? activo { get; set; }
        public string? ImagenBase64 { get; set; }
    }

    public class ImagenProducto
    {
        [Key]
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int tenantId { get; set; }

        [Required]
        public string NombreImagen { get; set; } = null!;

        [Required]
        public byte[] Imagen { get; set; } = null!;

        public Product Producto { get; set; } = null!;
    }
}
