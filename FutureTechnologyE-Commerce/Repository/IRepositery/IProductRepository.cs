using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
	public interface IProductRepository : IRepository<Product>
	{
		void Ubdate(Product product);
	}
}
