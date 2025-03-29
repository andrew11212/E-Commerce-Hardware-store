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
using NuGet.ContentModel;

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

		public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null,
								  string? includeProperties = null)
		{
			IQueryable<T> query = Set;

			if (filter != null)
				query = query.Where(filter);

			if (includeProperties != null)
			{
				foreach (var property in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
				{
					query = query.Include(property.Trim());
				}
			}

			return query.ToList();
		}

        public T Get(Expression<Func<T, bool>> filter, params string[] includeProperties)
        {
            IQueryable<T> query = Set;
            foreach (var property in includeProperties)
            {
                query = query.Include(property);
            }
            return query.FirstOrDefault(filter);
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
