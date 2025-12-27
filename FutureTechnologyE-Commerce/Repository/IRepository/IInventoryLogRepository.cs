using FutureTechnologyE_Commerce.Models;

namespace FutureTechnologyE_Commerce.Repository.IRepository
{
    public interface IInventoryLogRepository : IRepository<InventoryLog>
    {
        void Update(InventoryLog log);
        IEnumerable<InventoryLog> GetLogsByInventoryId(int inventoryId);
        IEnumerable<InventoryLog> GetRecentLogs(int count = 50);
    }
} 