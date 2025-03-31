using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Repository
{
	public class CategoryRepository : Repositery<Category>, ICategoryRepository
	{
		private readonly ApplicationDbContext context;

		public CategoryRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}


		public async Task UpdateAsync(Category category)
		{
			context.Attach(category);
			context.Entry(category).State = EntityState.Modified;
			// The actual saving to the database will be done in the UnitOfWork
			await Task.CompletedTask;
		}

	}
}
