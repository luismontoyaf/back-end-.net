using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Infrastructure.Data;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/users/")]
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly UserService _userService;

        public UserController(UserService userService, AppDbContext context)
        {
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _userRepository = new UserRepository(connectionString, context);
            _userService = userService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] Client client)
        {
            var result = _userService.RegisterUser(client);
            if (!result)
                return BadRequest(new { Message = "No se pudo registrar el usuario" });

            var result2 = _userService.RegisterEmploye(new Employe
            {
                nombre = client.nombre,
                apellidos = client.apellidos,
                tipoDocumento = client.tipoDocumento,
                numDocumento = client.numDocumento,
                correo = client.correo,
                fechaNacimiento = client.fechaNacimiento,
                fechaIngreso = DateTime.Today,
                rol = "Cliente", // Convertir de nuevo a string si es necesario
                estado = 1,
                contrasena = client.contrasena,
                celular = client.celular,
                direccion = client.direccion,
                genero = client.genero,
                clienteId = client.Id
            });
            if (result2)
                return Ok(new { Message = "Usuario registrado exitosamente" });

            return BadRequest(new { Message = "No se pudo registrar el usuario" });
        }

        [HttpPost("registerEmploye")]
        public IActionResult RegisterEmploye([FromBody] Employe employe)
        {
            var result = _userService.RegisterEmploye(employe);
            if (result)
                return Ok(new { Message = "Usuario registrado exitosamente" });

            return BadRequest(new { Message = "No se pudo registrar el usuario" });
        }
    }
}
