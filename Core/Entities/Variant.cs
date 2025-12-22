using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Variant
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JsonValues { get; set; } = string.Empty;
        public bool State { get; set; }
    }

    public class VariantDto
    {
        public string Name { get; set; }
        public List<AtributeDto> Atributes { get; set; }
    }

    public class AtributeDto
    {
        public string Value { get; set; }
    }
}