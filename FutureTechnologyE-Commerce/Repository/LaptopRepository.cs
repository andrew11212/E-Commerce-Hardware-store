using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepositery;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Repository
{
	public class LaptopRepository : Repositery<Laptop>, IlaptopRepository
	{
		private readonly ApplicationDbContext context;

		public LaptopRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}
		public async Task UpdateAsync(Laptop laptop)
		{
			context.Attach(laptop);
			context.Entry(laptop).State = EntityState.Modified;
			// The actual saving to the database will be done in the UnitOfWork
			await Task.CompletedTask;
		}
	}
}
