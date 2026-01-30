using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public DateTime? fechaNacimiento { get; set; }
        [NotMapped]  // Esta propiedad no se almacenará en la BD
        public required string contrasena { get; set; }
        public required string celular { get; set; }
        public required string direccion { get; set; }
        public string? genero { get; set; }
        public int? estado { get; set; }
    }

    public class ClientDto
    {

        [Key]
        public int? Id { get; set; }
        public string? nombre { get; set; }
        public string? apellidos { get; set; }
        public string? tipoDocumento { get; set; }
        public string? numDocumento { get; set; }
        public string? correo { get; set; }
        public DateTime? fechaNacimiento { get; set; }
        [NotMapped]  // Esta propiedad no se almacenará en la BD
        public string? contrasena { get; set; }
        public string? celular { get; set; }
        public string? direccion { get; set; }
        public string? genero { get; set; }
        public int? estado { get; set; }
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
        public required int rol { get; set; }
        public int? estado { get; set; }
        public required string contrasena { get; set; }
        public required string celular { get; set; }
        public required string direccion { get; set; }
        public string? genero { get; set; }
        public int? clienteId { get; set; }
    }

    public class EmployeDto
    {

        [Key]
        public int? Id { get; set; }
        public string? nombre { get; set; }
        public string? apellidos { get; set; }
        public string? tipoDocumento { get; set; }
        public string? numDocumento { get; set; }
        public string? correo { get; set; }
        public DateTime? fechaNacimiento { get; set; }
        public  DateTime? fechaIngreso { get; set; }
        public string? rol { get; set; }
        public int? estado { get; set; }
        public string? contrasena { get; set; }
        public string? celular { get; set; }
        public string? direccion { get; set; }
        public string? genero { get; set; }
        public int? clienteId { get; set; }
    }

    public class UpdateUserRequest
    {
        public EmployeDto Data { get; set; }
    }

    public class UpdateClientRequest
    {
        public ClientDto Data { get; set; }
    }
}