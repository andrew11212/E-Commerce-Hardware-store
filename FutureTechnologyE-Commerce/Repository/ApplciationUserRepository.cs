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
	public class ApplciationUserRepository : Repositery<ApplicationUser>, IApplciationUserRepository
	{
		private readonly ApplicationDbContext context;

		public ApplciationUserRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}
		public async Task UpdateAsync(ApplicationUser user)
		{
			context.Attach(user);
			context.Entry(user).State = EntityState.Modified;
			// The actual saving to the database will be done in the UnitOfWork
			await Task.CompletedTask;
		}
	}
}
