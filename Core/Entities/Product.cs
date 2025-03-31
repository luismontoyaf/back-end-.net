using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Product
    {
        [Key]
        public required int Id { get; set; }
        public required string nombreProducto { get; set; }
        public required string descripcion { get; set; }
        public required int stock { get; set; }
        public required decimal precio { get; set; }
        [NotMapped]
        public string? ImagenBase64 { get; set; } // Para enviar al frontend
        [NotMapped]
        public IFormFile? ImagenFile { get; set; } // Para recibir archivos al agregar
        public int? activo { get; set; } = 1;
    }
}
