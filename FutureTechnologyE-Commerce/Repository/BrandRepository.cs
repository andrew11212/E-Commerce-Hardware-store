using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepositery;

namespace FutureTechnologyE_Commerce.Repository
{
    public class BrandRepository:Repositery<Brand>, IBrandRepository
    {
        public BrandRepository(ApplicationDbContext db) : base(db)
        {

        }
    }

    }
