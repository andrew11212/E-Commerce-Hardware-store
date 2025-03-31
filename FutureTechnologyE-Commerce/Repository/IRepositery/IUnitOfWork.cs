using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepositery;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
	public interface IUnitOfWork:IDisposable
	{
        public ICategoryRepository CategoryRepository { get; }
        public IProductRepository ProductRepository { get; }
		public IProductTypeRepository ProductTypeRepository { get; }

		public IBrandRepository BrandRepository { get; }
        public IShopingCartRepositery CartRepositery { get; }
		public IlaptopRepository LaptopRepository { get; }
		public IApplciationUserRepository applciationUserRepository { get; }
		public IOrderHeaderRepository OrderHeader { get; }
		public IOrderDetailRepository OrderDetail { get; }

		IDbContextTransaction BeginTransaction(); // Add this line
		public Task SaveAsync();
	}
}
