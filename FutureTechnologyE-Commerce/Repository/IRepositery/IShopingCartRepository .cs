using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
    public interface IShopingCartRepositery : IRepository<ShopingCart>
	{
		Task UpdateAsync(ShopingCart shopingCart);

	}
}
