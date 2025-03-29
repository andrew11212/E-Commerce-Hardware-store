using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository
{
	public class ProductRepository : Repositery<Product>, IProductRepository
	{
		private readonly ApplicationDbContext context;

		public ProductRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}
		public void Ubdate(Product product)
		{
			context.Update(product);
		}

	}

}
