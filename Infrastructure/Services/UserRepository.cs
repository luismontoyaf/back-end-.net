using System;
using Core.Models;
using Microsoft.Data.SqlClient;
using Infrastructure.Data;
using Core.Interfaces;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;

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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, Nombre, Apellidos, Correo, Rol FROM Usuarios WHERE Correo = @Email";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
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

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Contrasena FROM Usuarios WHERE Correo = @Username";

                using (SqlCommand command = new SqlCommand(query, connection))
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

        public async Task DeleteRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token != null)
            {
                _context.UserRefreshTokens.Remove(token);
                await _context.SaveChangesAsync();
            }
        }
    }
}
