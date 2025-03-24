using System;
using Core.Models;
using Microsoft.Data.SqlClient;
using Infrastructure.Data;
using Core.Interfaces;


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

        public bool ValidateUser(string username, string password)
        {
            bool isValid = false;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(*) FROM Usuarios WHERE Username = @Username AND Password = @Password";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    isValid = count > 0;
                }
            }

            return isValid;
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
