using System.ComponentModel.DataAnnotations;

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
        public required IFormFile imagen { get; set; }
    }
}
