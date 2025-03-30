using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepositery;
using FutureTechnologyE_Commerce.Repository.IRepository;

namespace FutureTechnologyE_Commerce.Repository
{
	public class LaptopRepository : Repositery<Laptop>, IlaptopRepository
	{
		private readonly ApplicationDbContext context;

		public LaptopRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}
		public void Update(Laptop laptop)
		{
			context.Laptops.Update(laptop);
		}
	}
}
