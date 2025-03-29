using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;

namespace FutureTechnologyE_Commerce.Repository
{
    public class ProductTypeRepository:Repositery<ProductType>, IProductTypeRepository
    {
        public ProductTypeRepository(ApplicationDbContext db) : base(db)
        {
        }
    }
    
    
}
