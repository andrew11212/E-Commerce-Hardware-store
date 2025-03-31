using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Repository
{
	public class ProductRepository : Repositery<Product>, IProductRepository
	{
		private readonly ApplicationDbContext context;

		public ProductRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}
		public async Task UpdateAsync(Product product)
		{
			context.Attach(product);
			context.Entry(product).State = EntityState.Modified;
			// The actual saving to the database will be done in the UnitOfWork
			await Task.CompletedTask;
		}

	}

}
