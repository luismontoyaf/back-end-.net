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

        public EmployeDto GetUserByEmail(string email, int tenantId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
            SELECT id, nombre, apellidos, correo, rol, tenant_id 
            FROM usuarios 
            WHERE correo = @Email AND tenant_id = @TenantId";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeDto
                            {
                                Id = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                apellidos = reader.GetString(2),
                                correo = reader.GetString(3),
                                rol = reader.GetInt32(4).ToString(),
                                TenantId = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public bool ValidateUser(string username, string password, int tenantId)
        {
            string hashedPasswordFromDb = null;

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
            SELECT contrasena 
            FROM usuarios 
            WHERE correo = @Username AND tenant_id = @TenantId";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();
                    var result = command.ExecuteScalar();

                    if (result != null)
                        hashedPasswordFromDb = result.ToString();
                }
            }

            if (hashedPasswordFromDb == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password.Trim(), hashedPasswordFromDb);
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

        Task<Client> IUserRepository.GetClientByIdAsync(int id, int tenantId)
        {
            return _context.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        }

        Task<List<Client>> IUserRepository.GetAllClientsAsync()
        {
            return _context.Clientes.ToListAsync();
        }

        Task<List<EmployeDto>> IUserRepository.GetUsers(int tenantId)
        {
            return _context.Usuarios
                .Where(u => u.TenantId == tenantId)
                .Select(u => new EmployeDto
                {
                Id = u.Id,
                nombre = u.nombre,
                apellidos = u.apellidos,
                tipoDocumento = u.tipoDocumento,
                numDocumento = u.numDocumento,
                correo = u.correo,
                fechaNacimiento = u.fechaNacimiento,
                fechaIngreso = u.fechaIngreso,
                rol = u.rol.ToString(),
                estado = u.estado,
                contrasena = u.contrasena,
                celular = u.celular,
                direccion = u.direccion,
                genero = u.genero,
                clienteId = u.clienteId,
                TenantId = u.TenantId
                }).ToListAsync();
        }

        Task<List<ClientDto>> IUserRepository.GetClients(int tenantId)
        {
            return _context.Clientes
                .Where(u => u.TenantId == tenantId)
                .Select(u => new ClientDto
                {
                    Id = u.Id,
                    nombre = u.nombre,
                    apellidos = u.apellidos,
                    tipoDocumento = u.tipoDocumento,
                    numDocumento = u.numDocumento,
                    correo = u.correo,
                    fechaNacimiento = u.fechaNacimiento,
                    estado = u.estado,
                    contrasena = u.contrasena,
                    celular = u.celular,
                    direccion = u.direccion,
                    genero = u.genero,
                    TenantId = u.TenantId
                }).ToListAsync();
        }

        async Task<Employe?> IUserRepository.GetUserByIdAsync(int id, int tenantId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        }

        async Task<Employe?> IUserRepository.GetUserByIdClientAsync(int id, int tenantId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.clienteId == id && u.TenantId == tenantId);
        }

        void IUserRepository.Update(Employe user)
        {
            _context.Usuarios.Update(user); // Marca todo como modificado
        }

        void IUserRepository.UpdateClient(Client client)
        {
            _context.Clientes.Update(client); // Marca todo como modificado
        }

        public async Task SaveRefreshTokenAsync(int userId, int tenantId, string refreshToken, DateTime expiryDate)
        {
            var token = new UserRefreshToken
            {
                UserId = userId,
                TenantId = tenantId,
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

        public async Task DeleteRefreshTokenByUserAsync(int userId, int tenantId)
        {
            var tokens = _context.UserRefreshTokens
                .Where(rt => rt.UserId == userId && rt.TenantId == tenantId);

             _context.UserRefreshTokens.RemoveRange(tokens);
             await _context.SaveChangesAsync();
        }
    }
}
