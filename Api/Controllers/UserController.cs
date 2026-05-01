using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/users/")]
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly IUserRepository _userIRepository;
        private readonly UserService _userService;
        private readonly TenantProvider _tenantProvider;

        public UserController(UserService userService, 
            IUserRepository iUserRepository, 
            AppDbContext context, 
            IConfiguration configuration,
            TenantProvider tenantProvider)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            _userRepository = new UserRepository(connectionString, context);
            _userService = userService;
            _userIRepository = iUserRepository;
            _tenantProvider = tenantProvider;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] Client client)
        {
            var result = _userService.RegisterClient(client);
            if (!result)
                return BadRequest(new { Message = "No se pudo registrar el usuario" });

            var result2 = _userService.RegisterEmploye(new EmployeDto
            {
                Id = client.Id,
                nombre = client.nombre,
                apellidos = client.apellidos,
                tipoDocumento = client.tipoDocumento,
                numDocumento = client.numDocumento,
                correo = client.correo,
                fechaNacimiento = client.fechaNacimiento,
                fechaIngreso = DateTime.Today,
                rol = "Cliente",
                estado = 1,
                contrasena = client.contrasena,
                celular = client.celular,
                direccion = client.direccion,
                genero = client.genero,
                clienteId = client.Id,
                TenantId = client.TenantId
            });

            if (result && result2)
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
            var usuarios = await _userService.GetUsers();

            return Ok(usuarios);
        }

        [HttpGet("getClients")]
        public async Task<IActionResult> GetClients()
        {
            var result = await _userService.GetClients();

            return Ok(result);
        }

        [HttpPut("updateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            await _userService.UpdateUserAsync(request.Data);

            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        [HttpPut("updateClient")]
        public async Task<IActionResult> UpdateClient([FromBody] UpdateClientRequest request)
        {
            await _userService.UpdateClientAsync(request.Data);

            var user = new EmployeDto
            {
                Id = request.Data.Id ?? 0,
                nombre = request.Data.nombre,
                apellidos = request.Data.apellidos,
                tipoDocumento = request.Data.tipoDocumento,
                numDocumento = request.Data.numDocumento,
                correo = request.Data.correo,
                fechaNacimiento = request.Data.fechaNacimiento,
                celular = request.Data.celular,
                direccion = request.Data.direccion,
                genero = request.Data.genero
            };

            await _userService.UpdateUserAsync(user);

            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        [HttpPut("changeStatusUser")]
        public async Task<IActionResult> ChangeStatusUser([FromBody] ChangeStatusRequest request)
        {
            var user = await _userService.ChangeStatusUserAsync(request);

            if (user.clienteId != null)
            {
                await _userService.ChangeStatusClientAsync(user.clienteId ?? 0);
            }

            return Ok(new { message = "Usuario desactivado correctamente" });
        }

    }
}
