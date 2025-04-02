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
		Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null,
									   string? includeProperties = null);
		IQueryable<T> GetQueryable(Expression<Func<T, bool>>? filter = null,
									   string? includeProperties = null);
		Task<T?> GetAsync(Expression<Func<T, bool>> filter, params string[] includeProperties);

		Task AddAsync(T entity);

		Task RemoveAsync(T entity);

		Task RemoveRangeAsync(IEnumerable<T> entity);
	}
}