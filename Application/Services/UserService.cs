using Core.Models;
using Core.Interfaces;
using BCrypt.Net;
using Infrastructure.Data;

namespace Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TenantProvider _tenantProvider;

        public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, TenantProvider tenantProvider)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _tenantProvider = tenantProvider;
        }

        public Task<List<EmployeDto>> GetUsers()
        {
            var tenantId = _tenantProvider.GetTenantId();

            return _userRepository.GetUsers(tenantId);
        }

        public Task<List<ClientDto>> GetClients()
        {
            var tenantId = _tenantProvider.GetTenantId();

            return _userRepository.GetClients(tenantId);
        }

        public bool RegisterClient(Client client)
        {
            if (string.IsNullOrEmpty(client.nombre) || string.IsNullOrEmpty(client.celular))
                throw new ArgumentException("Datos inválidos");

            client.estado = 1;
            client.TenantId = _tenantProvider.GetTenantId();

            return _userRepository.CreateClient(client);
        }

        public bool RegisterEmploye(EmployeDto employe)
        {
            if (string.IsNullOrEmpty(employe.nombre) || string.IsNullOrEmpty(employe.celular))
                throw new ArgumentException("Datos inválidos");

            int tipoUsuario = VerifyTypeUser(employe.rol);

            employe.contrasena = HashPassword(employe.contrasena.Trim());

            return _userRepository.CreateEmploye(new Employe
            {
                nombre = employe.nombre,
                apellidos = employe.apellidos,
                tipoDocumento = employe.tipoDocumento,
                numDocumento = employe.numDocumento,
                correo = employe.correo,
                fechaNacimiento = employe.fechaNacimiento,
                fechaIngreso = employe.fechaIngreso ?? DateTime.Now,
                rol = tipoUsuario,
                estado = 1,
                contrasena = employe.contrasena,
                celular = employe.celular,
                direccion = employe.direccion,
                genero = employe.genero,
                clienteId = employe.clienteId,
                TenantId = _tenantProvider.GetTenantId()
            });
        }

        public async Task<Employe> ChangeStatusUserAsync(ChangeStatusRequest request)
        {
            var tenantId = _tenantProvider.GetTenantId();
            
            Employe user;

            if (request.typeUser == "Usuario")
            {
                user = await _userRepository.GetUserByIdAsync(request.UserId, tenantId);
            }
            else
            {
                user = await _userRepository.GetUserByIdClientAsync(request.UserId, tenantId);
            }

            if (user == null || user.TenantId != tenantId)
                throw new Exception("Usuario no encontrado");

            user.estado = user.estado == 1 ? 0 : 1;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return (user);
        }

        public async Task ChangeStatusClientAsync(int idClient)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var client = await _userRepository.GetClientByIdAsync(idClient, tenantId);

            if (client == null)
                throw new Exception("Usuario no encontrado");

            client.estado = client.estado == 1 ? 0 : 1;

            _userRepository.UpdateClient(client);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(EmployeDto userDto)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var user = await _unitOfWork.Usuarios.GetUserByIdAsync(userDto.Id, tenantId);

            if (user == null || user.TenantId != tenantId)
                throw new Exception("Usuario no encontrado");

            if (!string.IsNullOrWhiteSpace(userDto.nombre))
                user.nombre = userDto.nombre;

            if (!string.IsNullOrWhiteSpace(userDto.apellidos))
                user.apellidos = userDto.apellidos;

            if (userDto.fechaIngreso.HasValue)
                user.fechaIngreso = userDto.fechaIngreso.Value;

            if (userDto.fechaNacimiento.HasValue)
                user.fechaNacimiento = userDto.fechaNacimiento.Value;

            if (!string.IsNullOrWhiteSpace(userDto.rol))
                user.rol = VerifyTypeUser(userDto.rol);

            if (!string.IsNullOrWhiteSpace(userDto.celular))
                user.celular = userDto.celular;

            if (!string.IsNullOrWhiteSpace(userDto.direccion))
                user.direccion = userDto.direccion;

            if (!string.IsNullOrWhiteSpace(userDto.genero))
                user.genero = userDto.genero;

            if (!string.IsNullOrWhiteSpace(userDto.contrasena))
                user.contrasena = HashPassword(userDto.contrasena.Trim());

            _unitOfWork.Usuarios.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateClientAsync(ClientDto clientDto)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var client = await _unitOfWork.Clientes.GetClientByIdAsync(clientDto.Id ?? throw new("Id Nulo"), tenantId);

            if (client == null || client.TenantId != tenantId)
                throw new Exception("Usuario no encontrado");

            if (!string.IsNullOrWhiteSpace(clientDto.nombre))
                client.nombre = clientDto.nombre;

            if (!string.IsNullOrWhiteSpace(clientDto.apellidos))
                client.apellidos = clientDto.apellidos;

            if (!string.IsNullOrWhiteSpace(clientDto.tipoDocumento))
                client.tipoDocumento = clientDto.tipoDocumento;

            if (!string.IsNullOrWhiteSpace(clientDto.numDocumento))
                client.numDocumento = clientDto.numDocumento;

            if (!string.IsNullOrWhiteSpace(clientDto.correo))
                client.correo = clientDto.correo;

            if (clientDto.fechaNacimiento.HasValue)
                client.fechaNacimiento = clientDto.fechaNacimiento;

            if (!string.IsNullOrWhiteSpace(clientDto.celular))
                client.celular = clientDto.celular;

            if (!string.IsNullOrWhiteSpace(clientDto.direccion))
                client.direccion = clientDto.direccion;

            if (!string.IsNullOrWhiteSpace(clientDto.genero))
                client.genero = clientDto.genero;

            _unitOfWork.Clientes.UpdateClient(client);
            await _unitOfWork.SaveChangesAsync();
        }

        public int VerifyTypeUser(string typeUser)
        {
            return typeUser switch
            {
                "Administrador" => 1,
                "Cliente" => 2,
                _ => 0
            };
        }

        public string ConvertTypeUser(int typeUser)
        {
            return typeUser switch
            {
                1 => "Administrador",
                2 => "Cliente",
                _ => "Colaborador"
            };
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
    }
}
