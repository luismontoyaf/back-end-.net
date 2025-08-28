using Core.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Core.Interfaces
{
    public interface IReportRepository
    {
        bool AddProduct(Product product);
    }
}