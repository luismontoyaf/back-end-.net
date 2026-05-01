using Core.Models;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        bool ValidateUser(string username, string password, int tenantId);
        bool CreateClient(Client client);
        bool CreateEmploye(Employe employe);
        Task<Client> GetClientByIdAsync(int id, int tenantId);
        Task<List<Client>> GetAllClientsAsync();
        Task<List<EmployeDto>> GetUsers(int tenantId);
        Task<List<ClientDto>> GetClients(int tenantId);
        Task<Employe?> GetUserByIdAsync(int id, int tenantId);
        Task<Employe?> GetUserByIdClientAsync(int id, int tenantId);
        void Update(Employe user);
        void UpdateClient(Client client);
        Task SaveRefreshTokenAsync(int userId, int tenantId, string refreshToken, DateTime expiryDate);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task DeleteRefreshTokenByUserAsync(int userId, int tenantId);
    }
}