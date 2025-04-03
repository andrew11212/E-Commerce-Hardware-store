using FutureTechnologyE_Commerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
    public interface IPromotionRepository : IRepository.IRepository<Promotion>
    {
        IEnumerable<Promotion> GetActivePromotions();

        public Task UpdateAsync(Promotion promotion);


	}
} 