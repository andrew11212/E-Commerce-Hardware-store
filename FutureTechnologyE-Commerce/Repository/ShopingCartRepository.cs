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
	public class ShopingCartRepository : Repositery<ShopingCart>, IShopingCartRepositery
	{
		private readonly ApplicationDbContext context;

		public ShopingCartRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}


		public void Update(ShopingCart shopingCart)
		{
			context.shopingCarts.Update(shopingCart);
		}

	}
}
