using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository
{
	public class InventoryLogRepository : Repositery<InventoryLog>, IInventoryLogRepository
	{
		private readonly ApplicationDbContext _db;

		public InventoryLogRepository(ApplicationDbContext db) : base(db)
		{
			_db = db;
		}

		public IEnumerable<InventoryLog> GetLogsByInventoryId(int inventoryId)
		{
			return _db.Set<InventoryLog>()
				.Where(l => l.InventoryId == inventoryId)
				.OrderByDescending(l => l.ChangeDate)
				.ToList();
		}

		public IEnumerable<InventoryLog> GetRecentLogs(int count = 50)
		{
			return _db.Set<InventoryLog>()
				.Include(l => l.Inventory)
				.ThenInclude(i => i.Product)
				.OrderByDescending(l => l.ChangeDate)
				.Take(count)
				.ToList();
		}

		public void Update(InventoryLog log)
		{
			_db.Attach(log);
			_db.Entry(log).State = EntityState.Modified;
		}
	}
} 