using Core.Models;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        EmployeDto? GetUserByEmail(string email, int tenantId);
        Task<Employe?> GetActiveUserByEmailAsync(string email, int tenantId);
        bool ValidateUser(string username, string password, int tenantId);
        bool CreateClient(Client client);
        bool CreateEmploye(Employe employe);
        Task<Client> GetClientByIdAsync(int id, int tenantId);
        Task<Client> GetFinalCustomer();
        Task <List<Client>> GetAllClientsAsync();
        Task<List<EmployeDto>> GetUsers(int tenantId);
        Task<List<ClientDto>> GetClients(int tenantId);
        Task<Employe?> GetUserByIdAsync(int id, int tenantId);
        Task<Employe?> GetUserByIdClientAsync(int id, int tenantId);
        void Update(Employe user);
        void UpdateClient(Client client);
        Task SaveRefreshTokenAsync(int userId, int tenantId, string refreshToken, DateTime expiryDate);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task DeleteRefreshTokenByUserAsync(int userId, int tenantId);
        Task InvalidatePasswordResetTokensAsync(int userId, int tenantId);
        Task SavePasswordResetTokenAsync(PasswordResetToken token);
        Task<bool> CompletePasswordResetAsync(string tokenHash, int tenantId, string passwordHash, DateTime now);
    }
}