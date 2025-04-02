using FutureTechnologyE_Commerce.Models;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
    public interface IReviewRepository : IRepository<Review>
    {
        void Update(Review review);
        IEnumerable<Review> GetReviewsByProductId(int productId);
        double GetAverageRatingByProductId(int productId);
    }
} 