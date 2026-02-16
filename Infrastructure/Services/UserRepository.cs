using System;
using Core.Models;
using Microsoft.Data.SqlClient;
using Infrastructure.Data;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly  AppDbContext _context;
        private readonly string _connectionString;

        public UserRepository(string connectionString, AppDbContext context)
        {
            _connectionString = connectionString;
            _context = context;
        }

        public EmployeDto GetUserByEmail(string email)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT id, nombre, apellidos, correo, rol FROM usuarios WHERE correo = @Email";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeDto
                            {
                                Id = reader.GetInt32(0),
                                nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                apellidos = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                tipoDocumento = "", // Asignar un valor por defecto
                                numDocumento = "", // Asignar un valor por defecto
                                correo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                fechaIngreso = DateTime.Now, // Asignar un valor por defecto
                                rol = reader.IsDBNull(4) ? string.Empty : reader.GetInt32(4).ToString(),
                                contrasena = "", // Asignar un valor por defecto
                                celular = "", // Asignar un valor por defecto
                                direccion = "" // Asignar un valor por defecto
                            };
                        }
                    }
                }
            }

            return null; // Si el usuario no existe, devolvemos null
        }
        public bool ValidateUser(string username, string password)
        {
            string hashedPasswordFromDb = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT contrasena FROM usuarios WHERE correo = @Username";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        hashedPasswordFromDb = result.ToString();
                    }
                }
            }

            if (hashedPasswordFromDb == null)
                return false;

            return VerifyHashedPassword(hashedPasswordFromDb, password.Trim());
        }

        private bool VerifyHashedPassword(string hashedPassword, string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public bool CreateClient(Client client){
            _context.Clientes.Add(client);
            return _context.SaveChanges() > 0;
        }

        public bool CreateEmploye(Employe employe)
        {
            _context.Usuarios.Add(employe);
            return _context.SaveChanges() > 0;
        }

        Task<Client> IUserRepository.GetClientByIdAsync(int id)
        {
            return _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        }

        Task<List<Client>> IUserRepository.GetAllClientsAsync()
        {
            return _context.Clientes.ToListAsync();
        }

        Task<List<EmployeDto>> IUserRepository.GetUsers()
        {
            return _context.Usuarios.Select(u => new EmployeDto
            {
                Id = u.Id,
                nombre = u.nombre,
                apellidos = u.apellidos,
                tipoDocumento = u.tipoDocumento,
                numDocumento = u.numDocumento,
                correo = u.correo,
                fechaNacimiento = u.fechaNacimiento,
                fechaIngreso = u.fechaIngreso,
                rol = u.rol.ToString(), // <- conversión explícita
                estado = u.estado,
                contrasena = u.contrasena,
                celular = u.celular,
                direccion = u.direccion,
                genero = u.genero,
                clienteId = u.clienteId
            }).ToListAsync();
        }

        async Task<Employe?> IUserRepository.GetUserByIdAsync(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        void IUserRepository.Update(Employe user)
        {
            _context.Usuarios.Update(user); // Marca todo como modificado
        }

        void IUserRepository.UpdateClient(Client client)
        {
            _context.Clientes.Update(client); // Marca todo como modificado
        }

        public async Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate)
        {
            var token = new UserRefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = expiryDate
            };

            _context.UserRefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<UserRefreshToken?> GetRefreshTokenAsync(string refreshToken)
        {
            return await _context.UserRefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        }

        public async Task DeleteRefreshTokenByUserAsync(int userId)
        {
            var tokens = _context.UserRefreshTokens
                .Where(rt => rt.UserId == userId);

             _context.UserRefreshTokens.RemoveRange(tokens);
             await _context.SaveChangesAsync();
        }
    }
}
