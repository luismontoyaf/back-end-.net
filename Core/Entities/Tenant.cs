using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class Tenant
    {
        [Key]
        public int Id { get; set; }
        public required string nombre { get; set; }
        public required string identificador { get; set; }
        public string? nit { get; set; }
        public string ? direccion { get; set; }
        public string ? celular { get; set; }
        public string ? correo { get; set; }
        public bool estado { get; set; }
        public DateTime? fechaCreacion { get; set; }
    }
}
