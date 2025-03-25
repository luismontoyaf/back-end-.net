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
