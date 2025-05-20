using Core.Models;

namespace Core.Interfaces
{
    public interface IUserRepository
    {
        bool ValidateUser(string username, string password);
        bool CreateUser(Client client);
        bool CreateEmploye(Employe employe);
        Task<Client> GetClientByDocumentAsync(string id);
    }
}