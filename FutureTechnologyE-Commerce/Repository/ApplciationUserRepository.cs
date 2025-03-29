using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Repository.IRepository;
namespace FutureTechnologyE_Commerce.Repository
{
	public class ApplciationUserRepository : Repositery<ApplicationUser>, IApplciationUserRepository
	{
		private readonly ApplicationDbContext context;

		public ApplciationUserRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}

	}
}
