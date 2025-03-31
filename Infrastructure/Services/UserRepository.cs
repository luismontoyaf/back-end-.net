using System;
using Core.Models;
using Microsoft.Data.SqlClient;
using Infrastructure.Data;
using Core.Interfaces;
using BCrypt.Net;


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

        public Employe GetUserByEmail(string email)
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
                            return new Employe
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

        //public bool ValidateUser(string username, string password)
        //{
        //    bool isValid = false;
        //    using (SqlConnection connection = new SqlConnection(_connectionString))
        //    {
        //        string query = "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Username AND Contrasena = @Password";

        //        using (SqlCommand command = new SqlCommand(query, connection))
        //        {
        //            command.Parameters.AddWithValue("@Username", username);
        //            command.Parameters.AddWithValue("@Password", password);

        //            connection.Open();
        //            int count = (int)command.ExecuteScalar();
        //            isValid = count > 0;
        //        }
        //    }

        //    return isValid;
        //}

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

        public bool CreateUser(Client client){
            _context.Clientes.Add(client);
            return _context.SaveChanges() > 0;
        }

        public bool CreateEmploye(Employe employe)
        {
            _context.Usuarios.Add(employe);
            return _context.SaveChanges() > 0;
        }
    }
}
