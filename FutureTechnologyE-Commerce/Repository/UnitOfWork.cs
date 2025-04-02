using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepositery;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationDbContext context;
		public ICategoryRepository CategoryRepository { get; private set; }

		public IProductRepository ProductRepository { get; private set; }


		public IShopingCartRepositery CartRepositery { get; private set; }

		public IApplciationUserRepository applciationUserRepository { get; private set; }
		public IOrderHeaderRepository OrderHeader { get; private set; }
		public IOrderDetailRepository OrderDetail { get; private set; }

        public IBrandRepository BrandRepository { get; private set; }

		public IlaptopRepository LaptopRepository { get; private set; }
        
        public IReviewRepository ReviewRepository { get; private set; }

		public UnitOfWork(ApplicationDbContext context)
		{
			this.context = context;
			CategoryRepository = new CategoryRepository(context);

			ProductRepository = new ProductRepository(context);

			CartRepositery = new ShopingCartRepository(context);
			applciationUserRepository = new ApplciationUserRepository(context);

			OrderHeader = new OrderHeaderRepository(context);
			OrderDetail = new OrderDetailRepository(context);
            BrandRepository = new BrandRepository(context);
			LaptopRepository = new LaptopRepository(context);
            ReviewRepository = new ReviewRepository(context);
        }
		public async Task SaveAsync()
		{
			await context.SaveChangesAsync();
		}

		public void Dispose()
		{
			context.Dispose();
		}

		public IDbContextTransaction BeginTransaction() // Implement this method
		{
			return context.Database.BeginTransaction();
		}
	}
}
