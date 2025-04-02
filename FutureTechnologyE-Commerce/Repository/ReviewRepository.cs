using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FutureTechnologyE_Commerce.Repository
{
    public class ReviewRepository : Repositery<Review>, IReviewRepository
    {
        private readonly ApplicationDbContext _db;

        public ReviewRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Review review)
        {
            _db.Reviews.Update(review);
        }

        public IEnumerable<Review> GetReviewsByProductId(int productId)
        {
            return _db.Reviews.Where(r => r.ProductID == productId)
                             .Include(r => r.User)
                             .OrderByDescending(r => r.ReviewDate)
                             .ToList();
        }

        public double GetAverageRatingByProductId(int productId)
        {
            var ratings = _db.Reviews.Where(r => r.ProductID == productId).Select(r => r.Rating);
            return ratings.Any() ? ratings.Average() : 0;
        }
    }
} 