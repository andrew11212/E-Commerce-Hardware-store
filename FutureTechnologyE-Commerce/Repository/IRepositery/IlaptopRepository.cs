using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;

namespace FutureTechnologyE_Commerce.Repository.IRepositery
{
	public interface IlaptopRepository:IRepository<Laptop>
	{
		public Task UpdateAsync(Laptop laptop);
	}
}
