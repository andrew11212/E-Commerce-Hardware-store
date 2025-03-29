using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
	public interface IUnitOfWork
	{
		ICategoryRepository CategoryRepository { get; }
		IProductRepository ProductRepository { get; }

		public IShopingCartRepositery CartRepositery { get; }

		public IApplciationUserRepository applciationUserRepository { get; }
		public IOrderHeaderRepository OrderHeader { get; }
		public IOrderDetailRepository OrderDetail { get; }
		void Save();
	}
}
