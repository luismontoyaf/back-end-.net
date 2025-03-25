using Core.Models;
using Core.Interfaces;
using BCrypt.Net;

namespace Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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

        public bool RegisterEmploye(Employe employe)
        {
            // Validación de reglas de negocio, como contraseñas que coincidan
            if (string.IsNullOrEmpty(employe.nombre) || string.IsNullOrEmpty(employe.celular))
            {
                throw new ArgumentException("Datos inválidos");
            }

            // Convertir el rol de string a int
            employe.rol = employe.rol == "Administrador" ? "1" : "0";
            int tipoUsuario = int.Parse(employe.rol);

            // Hash de la contraseña
            employe.contrasena = HashPassword(employe.contrasena.Trim());

            // Llama al repositorio para guardar el usuario
            return _userRepository.CreateEmploye(new Employe
            {
                Id = employe.Id,
                nombre = employe.nombre,
                apellidos = employe.apellidos,
                tipoDocumento = employe.tipoDocumento,
                numDocumento = employe.numDocumento,
                correo = employe.correo,
                fechaNacimiento = employe.fechaNacimiento,
                fechaIngreso = employe.fechaIngreso,
                rol = tipoUsuario.ToString(), // Convertir de nuevo a string si es necesario
                estado = 1,
                contrasena = employe.contrasena,
                celular = employe.celular,
                direccion = employe.direccion,
                genero = employe.genero,
                clienteId = employe.clienteId
            });
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        //private string HashPassword(string password)
        //{
        //    byte[] salt = new byte[16];
        //    using (var rng = RandomNumberGenerator.Create())
        //    {
        //        rng.GetBytes(salt);
        //    }

        //    string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        //        password: password,
        //        salt: salt,
        //        prf: KeyDerivationPrf.HMACSHA256,
        //        iterationCount: 10000,
        //        numBytesRequested: 32
        //    ));

        //    return $"{Convert.ToBase64String(salt)}:{hashed}";
        //}
    }
}
