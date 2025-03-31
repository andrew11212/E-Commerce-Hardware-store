using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;

namespace FutureTechnologyE_Commerce.Repository.IRepositery
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Task UpdateAsync(Brand brand);
    }
}
