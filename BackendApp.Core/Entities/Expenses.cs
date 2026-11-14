using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class Egreso
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public int TipoEgresoId { get; set; }
        public TipoEgreso TipoEgreso { get; set; }

        public string? Descripcion { get; set; }

        public decimal Valor { get; set; }

        public DateTime Fecha { get; set; }

        public string? Referencia { get; set; }

        public int? CreadoPor { get; set; }
        public Employe? UsuarioRadica { get; set; }
    }

    public class TipoEgreso
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public ICollection<Egreso> Egresos { get; set; }
    }

    public class CreateExpenseDto
    {
        public int TipoEgresoId { get; set; }
        public decimal Valor { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
    }

    public class ExpenseDto
    {
        public int Id { get; set; }

        public string TipoEgreso { get; set; }

        public decimal Valor { get; set; }

        public string? Descripcion { get; set; }

        public DateTime Fecha { get; set; }

        public string? Referencia { get; set; }
        public int UsuarioRadicaId { get; set; }
        public string? UsuarioRadicaNombre { get; set; }
    }
}
