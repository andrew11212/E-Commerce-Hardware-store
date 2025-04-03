using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Repository
{
    public class InventoryRepository : Repositery<Inventory>, IInventoryRepository
    {
        private readonly ApplicationDbContext _db;

        public InventoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public IEnumerable<Inventory> GetAllWithProducts()
        {
            return _db.Set<Inventory>().Include(i => i.Product)
                .ThenInclude(p => p.Category)
                .ToList();
        }

        public Inventory? GetByProductId(int productId)
        {
            return _db.Set<Inventory>()
                .Include(i => i.Product)
                .FirstOrDefault(i => i.ProductId == productId);
        }

        public IEnumerable<Inventory> GetLowStockItems()
        {
            return _db.Set<Inventory>()
                .Include(i => i.Product)
                .Where(i => i.CurrentStock <= i.LowStockThreshold)
                .ToList();
        }

        public async Task<bool> UpdateStockQuantity(int productId, int newQuantity, string notes = "")
        {
            var inventory = GetByProductId(productId);
            if (inventory == null)
            {
                // Create new inventory record if it doesn't exist
                var product = _db.Products.Find(productId);
                if (product == null) return false;

                inventory = new Inventory
                {
                    ProductId = productId,
                    CurrentStock = newQuantity,
                    LowStockThreshold = 10, // Default value
                    Notes = notes
                };
                
                await _db.Set<Inventory>().AddAsync(inventory);
                await _db.SaveChangesAsync();
                return true;
            }

            // Create log entry
            var log = new InventoryLog
            {
                InventoryId = inventory.InventoryId,
                PreviousStock = inventory.CurrentStock,
                NewStock = newQuantity,
                ChangeQuantity = newQuantity - inventory.CurrentStock,
                ChangeType = newQuantity > inventory.CurrentStock ? "Addition" : "Reduction",
                Notes = notes
            };

            // Update inventory
            inventory.CurrentStock = newQuantity;
            inventory.LastUpdated = DateTime.Now;
            if (newQuantity > inventory.CurrentStock)
            {
                inventory.LastRestockDate = DateTime.Now;
            }
            inventory.Notes = notes;

            Update(inventory);
            await _db.Set<InventoryLog>().AddAsync(log);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdjustStock(int productId, int adjustmentQuantity, string changeType, string notes = "")
        {
            var inventory = GetByProductId(productId);
            if (inventory == null) return false;

            int newQuantity = inventory.CurrentStock + adjustmentQuantity;
            if (newQuantity < 0) newQuantity = 0; // Prevent negative stock

            // Create log entry
            var log = new InventoryLog
            {
                InventoryId = inventory.InventoryId,
                PreviousStock = inventory.CurrentStock,
                NewStock = newQuantity,
                ChangeQuantity = adjustmentQuantity,
                ChangeType = changeType,
                Notes = notes
            };

            // Update inventory
            inventory.CurrentStock = newQuantity;
            inventory.LastUpdated = DateTime.Now;
            if (adjustmentQuantity > 0)
            {
                inventory.LastRestockDate = DateTime.Now;
            }
            inventory.Notes = notes;

            Update(inventory);
            await _db.Set<InventoryLog>().AddAsync(log);
            await _db.SaveChangesAsync();
            return true;
        }

        public void Update(Inventory inventory)
        {
            _db.Attach(inventory);
            _db.Entry(inventory).State = EntityState.Modified;
        }
    }
} 