using Core.Models;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        bool ValidateUser(string username, string password);
        bool CreateClient(Client client);
        bool CreateEmploye(Employe employe);
        Task<Client> GetClientByIdAsync(int id);
        Task<List<Client>> GetAllClientsAsync();
        Task<List<EmployeDto>> GetUsers();
        Task<Employe?> GetUserByIdAsync(int id);
        void Update(Employe user);
        void UpdateClient(Client client);
        Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryDate);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task DeleteRefreshTokenAsync(string refreshToken);
    }
}