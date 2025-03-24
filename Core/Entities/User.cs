using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
 public class Client
    {

        [Key] 
        public int Id { get; set; }
        public required string nombre { get; set; }
        public required string apellidos { get; set; }
        public required string tipoDocumento { get; set; }
        public required string numDocumento { get; set; }
        public required string correo { get; set; }
        public required DateTime fechaNacimiento { get; set; }
        public required string contrasena { get; set; }
        public required string celular { get; set; }
        public required string direccion { get; set; }
        public string? genero { get; set; }
    }

    public class Employe
    {

        [Key]
        public int Id { get; set; }
        public required string nombre { get; set; }
        public required string apellidos { get; set; }
        public required string tipoDocumento { get; set; }
        public required string numDocumento { get; set; }
        public required string correo { get; set; }
        public DateTime? fechaNacimiento { get; set; }
        public required DateTime fechaIngreso { get; set; }
        public required string rol { get; set; }
        public int? estado { get; set; }
        public required string contrasena { get; set; }
        public required string celular { get; set; }
        public required string direccion { get; set; }
        public string? genero { get; set; }
    }
}