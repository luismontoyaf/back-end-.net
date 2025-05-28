using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Infrastructure.Data;
using Core.Interfaces;
using Microsoft.AspNetCore.JsonPatch;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/users/")]
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly IUserRepository _userIRepository;
        private readonly UserService _userService;

        public UserController(UserService userService, IUserRepository iUserRepository, AppDbContext context)
        {
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _userRepository = new UserRepository(connectionString, context);
            _userService = userService;
            _userIRepository = iUserRepository;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] Client client)
        {
            var result = _userService.RegisterUser(client);
            if (!result)
                return BadRequest(new { Message = "No se pudo registrar el usuario" });

            var result2 = _userService.RegisterEmploye(new EmployeDto
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
        public IActionResult RegisterEmploye([FromBody] EmployeDto employe)
        {
            var result = _userService.RegisterEmploye(employe);
            if (result)
                return Ok(new { Message = "Usuario registrado exitosamente" });

            return BadRequest(new { Message = "No se pudo registrar el usuario" });
        }

        [HttpGet("getUsers")]
        public async Task<IActionResult> GetUsers()
        {
            List <EmployeDto> usuarios = await _userIRepository.GetUsers();
            return Ok(usuarios);
        }

        [HttpPut("updateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            await _userService.UpdateUserAsync(request.Data);
            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        [HttpPut("changeStatusUser/{id}")]
        public async Task<IActionResult> ChangeStatusUser(int id)
        {
            await _userService.ChangeStatusUserAsync(id);
            return Ok(new { message = "Usuario desactivado correctamente" });
        }
    }
}
