using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Repository.IRepository;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository
{
	public class OrderDetailRepository : Repositery<OrderDetail>, IOrderDetailRepository
	{
		private readonly ApplicationDbContext context;

		public OrderDetailRepository(ApplicationDbContext context) : base(context)
		{
			this.context = context;
		}


		public void Update(OrderDetail order)
		{
			context.orderDetails.Update(order);
		}

	}
}
