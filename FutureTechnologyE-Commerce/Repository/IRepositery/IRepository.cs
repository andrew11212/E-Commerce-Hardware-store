using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
	public interface IRepository<T> where T : class
	{
		IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null,
								  string? includeProperties = null);

		T Get(Expression<Func<T, bool>> Filter, params string[] includes);

		void Add(T entity);

		void Remove(T entity);

		void RemoveRamge(IEnumerable<T> entity);


	}
}
