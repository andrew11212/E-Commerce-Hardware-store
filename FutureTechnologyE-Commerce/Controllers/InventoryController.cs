using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Linq;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Inventory
        public IActionResult Index()
        {
            var inventories = _unitOfWork.InventoryRepository.GetAllWithProducts();
            return View(inventories);
        }

        // GET: Inventory/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            // Include both Product and the Product's Category in the query
            var inventory = await _unitOfWork.InventoryRepository.GetAsync(i => i.InventoryId == id, "Product.Category", "Product.Brand");
            
            if (inventory == null)
            {
                TempData["error"] = "Inventory record not found";
                return RedirectToAction(nameof(Index));
            }

            if (inventory.Product == null)
            {
                // Get the product separately if it wasn't included
                var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == inventory.ProductId, "Category", "Brand");
                if (product == null)
                {
                    TempData["error"] = "Product not found for this inventory record";
                    return RedirectToAction(nameof(Index));
                }
                inventory.Product = product;
            }

            var inventoryLogs = _unitOfWork.InventoryLogRepository.GetLogsByInventoryId(id);
            
            var viewModel = new InventoryViewModel
            {
                Inventory = inventory,
                Product = inventory.Product,
                InventoryLogs = inventoryLogs ?? new List<InventoryLog>()
            };

            return View(viewModel);
        }

        // GET: Inventory/Create
        public async Task<IActionResult> Create()
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync();
            var viewModel = new InventoryViewModel
            {
                ProductList = products.Select(p => new SelectListItem
                {
                    Text = p.Name,
                    Value = p.ProductID.ToString()
                })
            };
            return View(viewModel);
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check if inventory already exists for this product
                var existingInventory = _unitOfWork.InventoryRepository.GetByProductId(viewModel.Inventory.ProductId);
                if (existingInventory != null)
                {
                    ModelState.AddModelError("Inventory.ProductId", "Inventory record already exists for this product.");
                    var products = await _unitOfWork.ProductRepository.GetAllAsync();
                    viewModel.ProductList = products.Select(p => new SelectListItem
                    {
                        Text = p.Name,
                        Value = p.ProductID.ToString()
                    });
                    return View(viewModel);
                }

                // Create inventory record
                await _unitOfWork.InventoryRepository.AddAsync(viewModel.Inventory);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Inventory created successfully";
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            var allProducts = await _unitOfWork.ProductRepository.GetAllAsync();
            viewModel.ProductList = allProducts.Select(p => new SelectListItem
            {
                Text = p.Name,
                Value = p.ProductID.ToString()
            });
            return View(viewModel);
        }

        // GET: Inventory/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            // Include both Product and the Product's relationships in the query
            var inventory = await _unitOfWork.InventoryRepository.GetAsync(i => i.InventoryId == id, "Product.Category", "Product.Brand");
            
            if (inventory == null)
            {
                TempData["error"] = "Inventory record not found";
                return RedirectToAction(nameof(Index));
            }

            if (inventory.Product == null)
            {
                // Get the product separately if it wasn't included
                var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == inventory.ProductId, "Category", "Brand");
                if (product == null)
                {
                    TempData["error"] = "Product not found for this inventory record";
                    return RedirectToAction(nameof(Index));
                }
                inventory.Product = product;
            }

            var viewModel = new InventoryViewModel
            {
                Inventory = inventory,
                Product = inventory.Product
            };

            return View(viewModel);
        }

        // POST: Inventory/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InventoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                viewModel.Inventory.LastUpdated = DateTime.Now;
                _unitOfWork.InventoryRepository.Update(viewModel.Inventory);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Inventory updated successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Inventory/AdjustStock/{id}
        public async Task<IActionResult> AdjustStock(int id)
        {
            // Include both Product and the Product's relationships in the query
            var inventory = await _unitOfWork.InventoryRepository.GetAsync(i => i.InventoryId == id, "Product.Category", "Product.Brand");
            
            if (inventory == null)
            {
                TempData["error"] = "Inventory record not found";
                return RedirectToAction(nameof(Index));
            }

            if (inventory.Product == null)
            {
                // Get the product separately if it wasn't included
                var product = await _unitOfWork.ProductRepository.GetAsync(p => p.ProductID == inventory.ProductId, "Category", "Brand");
                if (product == null)
                {
                    TempData["error"] = "Product not found for this inventory record";
                    return RedirectToAction(nameof(Index));
                }
                inventory.Product = product;
            }

            var viewModel = new InventoryViewModel
            {
                Inventory = inventory,
                Product = inventory.Product
            };

            return View(viewModel);
        }

        // POST: Inventory/AdjustStock/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, int adjustmentQuantity, string changeType, string notes)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetAsync(i => i.InventoryId == id);
            if (inventory == null)
            {
                return NotFound();
            }

            await _unitOfWork.InventoryRepository.AdjustStock(inventory.ProductId, adjustmentQuantity, changeType, notes);
            TempData["success"] = "Stock adjusted successfully";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Inventory/BulkUpdate
        public IActionResult BulkUpdate()
        {
            var inventories = _unitOfWork.InventoryRepository.GetAllWithProducts();
            var viewModel = new BulkInventoryUpdateViewModel
            {
                Items = inventories.Select(i => new InventoryItemViewModel
                {
                    InventoryId = i.InventoryId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    CurrentStock = i.CurrentStock,
                    NewStock = i.CurrentStock,
                    LowStockThreshold = i.LowStockThreshold
                }).ToList()
            };
            return View(viewModel);
        }

        // POST: Inventory/BulkUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdate(BulkInventoryUpdateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                foreach (var item in viewModel.Items)
                {
                    if (item.CurrentStock != item.NewStock)
                    {
                        var inventory = await _unitOfWork.InventoryRepository.GetAsync(i => i.InventoryId == item.InventoryId);
                        if (inventory != null)
                        {
                            await _unitOfWork.InventoryRepository.UpdateStockQuantity(inventory.ProductId, item.NewStock, item.Notes ?? "Bulk update");
                        }
                    }
                }
                TempData["success"] = "Inventory updated in bulk successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Inventory/LowStock
        public IActionResult LowStock()
        {
            var lowStockItems = _unitOfWork.InventoryRepository.GetLowStockItems();
            var viewModel = new LowStockViewModel
            {
                LowStockItems = lowStockItems.Select(i => new InventoryItemViewModel
                {
                    InventoryId = i.InventoryId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    CurrentStock = i.CurrentStock,
                    LowStockThreshold = i.LowStockThreshold
                }).ToList()
            };
            return View(viewModel);
        }

        // GET: Inventory/SyncWithProducts
        public async Task<IActionResult> SyncWithProducts()
        {
            // Get all products
            var products = await _unitOfWork.ProductRepository.GetAllAsync();
            int syncCount = 0;

            foreach (var product in products)
            {
                // Check if inventory exists for this product
                var inventory = _unitOfWork.InventoryRepository.GetByProductId(product.ProductID);
                if (inventory == null)
                {
                    // Create new inventory record
                    var newInventory = new Inventory
                    {
                        ProductId = product.ProductID,
                        CurrentStock = product.StockQuantity,
                        LowStockThreshold = 10,
                        Notes = "Auto-created from product sync"
                    };

                    await _unitOfWork.InventoryRepository.AddAsync(newInventory);
                    syncCount++;
                }
            }

            await _unitOfWork.SaveAsync();
            TempData["success"] = $"Synced {syncCount} products with inventory";
            return RedirectToAction(nameof(Index));
        }
    }
} 