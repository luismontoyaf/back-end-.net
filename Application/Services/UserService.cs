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

        public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public bool RegisterUser(Client client)
        {
            // Validación de reglas de negocio, como contraseñas que coincidan
            if (string.IsNullOrEmpty(client.nombre) || string.IsNullOrEmpty(client.celular))
            {
                throw new ArgumentException("Datos inválidos");
            }

            client.estado = 1;

            // Llama al repositorio para guardar el usuario
            return _userRepository.CreateUser(client);
        }

        public bool RegisterEmploye(EmployeDto employe)
        {
            // Validación de reglas de negocio, como contraseñas que coincidan
            if (string.IsNullOrEmpty(employe.nombre) || string.IsNullOrEmpty(employe.celular))
            {
                throw new ArgumentException("Datos inválidos");
            }

            // Convertir el rol de string a int
            //employe.rol = employe.rol == "Administrador" ? "1" : "0";

            int tipoUsuario = VerifyTypeUser(employe.rol);

            // Hash de la contraseña
            employe.contrasena = HashPassword(employe.contrasena.Trim());

            // Llama al repositorio para guardar el usuario
            return _userRepository.CreateEmploye(new Employe
            {
                Id = employe.Id ?? throw new("El id no puede ser nulo"),
                nombre = employe.nombre,
                apellidos = employe.apellidos,
                tipoDocumento = employe.tipoDocumento,
                numDocumento = employe.numDocumento,
                correo = employe.correo,
                fechaNacimiento = employe.fechaNacimiento,
                fechaIngreso = employe.fechaIngreso ?? DateTime.Now,
                rol = tipoUsuario, // Convertir de nuevo a string si es necesario
                estado = 1,
                contrasena = employe.contrasena,
                celular = employe.celular,
                direccion = employe.direccion,
                genero = employe.genero,
                clienteId = employe.clienteId
            });
        }

        public async Task ChangeStatusUserAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                throw new Exception("Usuario no encontrado");
            user.estado = user.estado == 1 ? 0 : 1;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(EmployeDto userDto)
        {
            var user = await _unitOfWork.Usuarios.GetUserByIdAsync(userDto.Id ?? throw new("Id Nulo"));
            if (user == null)
                throw new Exception("Usuario no encontrado");

            // Solo actualizar los campos no nulos
            if (!string.IsNullOrWhiteSpace(userDto.nombre))
                user.nombre = userDto.nombre;

            if (!string.IsNullOrWhiteSpace(userDto.apellidos))
                user.apellidos = userDto.apellidos;

            if (userDto.fechaIngreso.HasValue)
                user.fechaIngreso = userDto.fechaIngreso ?? DateTime.Now;

            if (userDto.fechaNacimiento.HasValue)
                user.fechaNacimiento = userDto.fechaNacimiento ?? DateTime.Now;

            if (!string.IsNullOrWhiteSpace(userDto.rol))
                user.rol = VerifyTypeUser(userDto.rol);

            if (!string.IsNullOrWhiteSpace(userDto.celular))
                user.celular = userDto.celular;

            if (!string.IsNullOrWhiteSpace(userDto.direccion))
                user.direccion = userDto.direccion;

            if (!string.IsNullOrWhiteSpace(userDto.genero))
                user.genero = userDto.genero;

            if (!string.IsNullOrWhiteSpace(userDto.contrasena))
                user.contrasena = HashPassword(userDto.contrasena.Trim()); // si usas hashing

            _unitOfWork.Usuarios.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public int VerifyTypeUser(string typeUser)
        {
            int typeUserInt = 0;
            switch (typeUser)
            {
                case "Administrador":
                    typeUserInt = 1;
                    break;
                case "Cliente":
                    typeUserInt = 2;
                    break;
                default:
                    typeUserInt = 0;
                    break;
            }
            return typeUserInt;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
    }
}
