using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Repository
{
	public class Repositery<T> : IRepository<T> where T : class
	{
		private readonly ApplicationDbContext context;

		internal DbSet<T> Set;
		public Repositery(ApplicationDbContext context)
		{
			this.context = context;
			Set = context.Set<T>();
			//Categories = Dbset;
		}

		public IEnumerable<T> GetAll(Expression<Func<T, bool>>? Filter = null, params string[] includes)
		{
			IQueryable<T> query = Set;
			if (Filter != null)
			{
				query = query.Where(Filter);
			}
			foreach (var include in includes)
			{
				query = query.Include(include);
			}
			return query;
		}

		public T Get(Expression<Func<T, bool>> Filter, params string[] includes)
		{
			IQueryable<T> query = Set;

			query = query.Where(Filter);

			// Apply string-based includes
			foreach (var include in includes)
			{
				query = query.Include(include);
			}

			return query.FirstOrDefault();
		}

		public void Add(T entity)
		{
			Set.Add(entity);
		}

		public void Remove(T entity)
		{
			Set.Remove(entity);
		}

		public void RemoveRamge(IEnumerable<T> entity)
		{
			Set.RemoveRange(entity);
		}


	}
}
