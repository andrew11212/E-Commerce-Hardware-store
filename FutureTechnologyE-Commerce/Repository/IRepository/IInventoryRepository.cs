using FutureTechnologyE_Commerce.Models;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        void Update(Inventory inventory);
        IEnumerable<Inventory> GetAllWithProducts();
        Inventory? GetByProductId(int productId);
        IEnumerable<Inventory> GetLowStockItems();
        Task<bool> UpdateStockQuantity(int productId, int newQuantity, string notes = "");
        Task<bool> AdjustStock(int productId, int adjustmentQuantity, string changeType, string notes = "");
    }
} 