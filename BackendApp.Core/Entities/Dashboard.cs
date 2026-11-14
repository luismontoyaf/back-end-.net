using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class DashboardResponseDto
    {
        public decimal VentasHoy { get; set; }

        public int TotalVentas { get; set; }

        public int TotalClientes { get; set; }

        public int StockBajo { get; set; }

        public List<VentaSemanalDto> VentasSemana { get; set; } = [];
    }

    public class VentaSemanalDto
    {
        public string Dia { get; set; } = string.Empty;

        public decimal Total { get; set; }
    }
}
