using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class Tenant
    {
        [Key]
        public int Id { get; set; }
        public required string nombre { get; set; }
        public required string identificador { get; set; }
        public bool estado { get; set; }
        public DateTime? fechaCreacion { get; set; }
    }
}
