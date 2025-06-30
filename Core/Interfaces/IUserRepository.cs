using Core.Models;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        bool ValidateUser(string username, string password);
        bool CreateUser(Client client);
        bool CreateEmploye(Employe employe);
        Task<Client> GetClientByDocumentAsync(int id);
        Task<List<Client>> GetAllClientsAsync();
        Task<List<EmployeDto>> GetUsers();
        Task<Employe?> GetUserByIdAsync(int id);
        void Update(Employe user);
        Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task DeleteRefreshTokenAsync(string refreshToken);
    }
}