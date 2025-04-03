using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FutureTechnologyE_Commerce.Repository
{
    public class PromotionRepository : Repositery<Promotion>, IPromotionRepository
    {
        private readonly ApplicationDbContext _db;

        public PromotionRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public IEnumerable<Promotion> GetActivePromotions()
        {
            var today = DateTime.Now.Date;
            return _db.Promotions
                .Include(p => p.Product)
                .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
                .OrderBy(p => p.DisplayOrder)
                .ToList();
        }

		public async Task UpdateAsync(Promotion promotion)
		{
			_db.Attach(promotion);
			_db.Entry(promotion).State = EntityState.Modified;
			// The actual saving to the database will be done in the UnitOfWork
			await Task.CompletedTask;
		}
	}
} 