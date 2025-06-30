using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Sale
    {
        [Key]
        public int IdFactura { get; set; }
        public int IdCliente { get; set; }
        [NotMapped]
        public string? NombreCliente { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? NumeroFactura { get; set; } // opcional si es calculado
        public string JsonFactura { get; set; } = string.Empty;
        public string FormaPago { get; set; } = string.Empty;
        public DateTime? FechaCreacion { get; set; }
    }

}
