using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepositery;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Repository
{
    public class BrandRepository:Repositery<Brand>, IBrandRepository
    {
		private readonly ApplicationDbContext db;

		public BrandRepository(ApplicationDbContext db) : base(db)
        {
			this.db = db;
		}

		public async Task UpdateAsync(Brand brand)
		{
			 db.Attach(brand);
			db.Entry(brand).State = EntityState.Modified;
			// The actual saving to the database will be done in the UnitOfWork
			 await Task.CompletedTask;
		}
	}

    }
