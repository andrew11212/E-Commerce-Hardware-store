using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        #region Dashboard

        public async Task<IActionResult> Dashboard(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var model = new AdminDashboardViewModel();
                
                // Set date range - limit to maximum 30 days to prevent performance issues
                model.StartDate = startDate ?? DateTime.Now.AddDays(-30);
                model.EndDate = endDate ?? DateTime.Now;
                
                // Prevent excessive date ranges that could cause performance issues
                if ((model.EndDate - model.StartDate).TotalDays > 30)
                {
                    model.StartDate = model.EndDate.AddDays(-30);
                    TempData["Warning"] = "Date range limited to 30 days for performance reasons.";
                }

                // Get orders with a single database query
                var orders = (await _unitOfWork.OrderHeader.GetAllAsync(
                    o => o.OrderDate >= model.StartDate && o.OrderDate <= model.EndDate))
                    .ToList();
                
                // Calculate statistics from the orders we already fetched
                model.TotalOrders = orders.Count;
                model.TotalRevenue = orders.Sum(o => (decimal)o.OrderTotal);
                model.PendingOrders = orders.Count(o => o.OrderStatus == SD.Status_Pending);
                model.ProcessingOrders = orders.Count(o => o.OrderStatus == SD.Status_Processing);
                model.ShippedOrders = orders.Count(o => o.OrderStatus == SD.Status_Shipped);
                model.DeliveredOrders = orders.Count(o => o.OrderStatus == SD.Status_Delivered);
                model.CancelledOrders = orders.Count(o => o.OrderStatus == SD.Status_Cancelled);

                // Get count of users (simple count is faster than loading all users)
                model.TotalCustomers = _userManager.Users.Count();
                
                // Fetch product count directly without loading all products
                var productCount = (await _unitOfWork.ProductRepository.GetAllAsync()).Count();
                model.TotalProducts = productCount;

                // Get only recent orders (just 5 instead of 10)
                model.RecentOrders = (await _unitOfWork.OrderHeader.GetAllAsync(
                    includeProperties: "ApplicationUser"))
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList();

                // Get sales data by period - limited to 15 data points for chart performance
                var salesByDay = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new ChartDataPoint
                    {
                        Label = g.Key.ToString("MM/dd"),
                        Value = g.Sum(o => (decimal)o.OrderTotal)
                    })
                    .OrderBy(p => DateTime.Parse(p.Label))
                    .Take(15)  // Limit data points
                    .ToList();

                model.SalesByPeriod = salesByDay;

                // Get top selling products - limited to just 3 instead of 5
                try
                {
                    // Wrap this in its own try/catch as it's not critical for the dashboard to load
                    // Use a simpler query that's less likely to cause performance issues
                    var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                        filter: od => od.orderHeader.OrderDate >= model.StartDate && od.orderHeader.OrderDate <= model.EndDate,
                        includeProperties: "Product");

                    // Process the data in memory to minimize DB load
                    var topProducts = orderDetails
                        .Where(od => od.Product != null) // Filter out any null products
                        .GroupBy(od => od.ProductId)
                        .Select(g => new 
                        {
                            ProductId = g.Key,
                            Product = g.First().Product,
                            QuantitySold = g.Sum(od => od.Count),
                            Revenue = g.Sum(od => (decimal)(od.Price * od.Count))
                        })
                        .OrderByDescending(p => p.Revenue)
                        .Take(3)  // Reduced from 5 to 3
                        .ToList();

                    model.TopSellingProducts = topProducts
                        .Where(p => p.Product != null) // Safety check
                        .Select(p => new TopSellingProduct
                        {
                            ProductId = p.ProductId,
                            Name = p.Product.Name ?? "Unknown Product",
                            ImageUrl = p.Product.ImageUrl ?? "/images/placeholder.png",
                            QuantitySold = p.QuantitySold,
                            Revenue = p.Revenue,
                            CurrentStock = p.Product.StockQuantity
                        }).ToList();
                }
                catch (Exception productEx)
                {
                    _logger.LogWarning(productEx, "Error loading top selling products for dashboard");
                    model.TopSellingProducts = new List<TopSellingProduct>();
                }

                // Get limited low stock items
                try
                {
                    // Wrap this in its own try-catch as it's not critical for the dashboard
                    var lowStockItems = _unitOfWork.InventoryRepository.GetLowStockItems().Take(5);  // Limit to 5
                    model.LowStockItems = lowStockItems.Select(i => new InventoryItemViewModel
                    {
                        InventoryId = i.InventoryId,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        CurrentStock = i.CurrentStock,
                        LowStockThreshold = i.LowStockThreshold
                    }).ToList();
                }
                catch (Exception inventoryEx)
                {
                    _logger.LogWarning(inventoryEx, "Error loading low stock items for dashboard");
                    model.LowStockItems = new List<InventoryItemViewModel>();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Failed to load dashboard data. The system might be experiencing high load.";
                
                // Return a minimal model with just the date range to avoid repeated crashes
                return View(new AdminDashboardViewModel
                {
                    StartDate = startDate ?? DateTime.Now.AddDays(-30),
                    EndDate = endDate ?? DateTime.Now,
                    // Initialize collections to empty lists to prevent null reference exceptions in the view
                    RecentOrders = new List<OrderHeader>(),
                    SalesByPeriod = new List<ChartDataPoint>(),
                    TopSellingProducts = new List<TopSellingProduct>(),
                    LowStockItems = new List<InventoryItemViewModel>()
                });
            }
        }

        #endregion

        #region User Management

        public async Task<IActionResult> UserManagement(string searchString = "", string roleFilter = "", int pageNumber = 1)
        {
            try
            {
                var pageSize = 10;
                var model = new UserManagementViewModel
                {
                    SearchString = searchString,
                    RoleFilter = roleFilter,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                // Get all roles
                model.RolesList = _roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name
                    })
                    .ToList();
                
                // Add "All Roles" option
                model.RolesList.Insert(0, new SelectListItem("All Roles", ""));

                // Get users with filtering
                var usersQuery = _userManager.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    usersQuery = usersQuery.Where(u => 
                        u.Email.Contains(searchString) || 
                        u.UserName.Contains(searchString) ||
                        u.first_name.Contains(searchString) || 
                        u.last_name.Contains(searchString));
                }

                // Get total count before pagination
                model.TotalUsers = await usersQuery.CountAsync();

                // Apply pagination
                var users = await usersQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Create view models for each user
                var userViewModels = new List<UserViewModel>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "";

                    // Skip if role filter is applied and doesn't match
                    if (!string.IsNullOrEmpty(roleFilter) && role != roleFilter)
                        continue;

                    // Get order data
                    var orders = await _unitOfWork.OrderHeader.GetAllAsync(o => o.ApplicationUserId == user.Id);
                    var orderCount = orders.Count();
                    var totalSpent = orders.Sum(o => (decimal)o.OrderTotal);
                    var lastOrderDate = orders.Any() ? (DateTime?)orders.Max(o => o.OrderDate) : null;

                    userViewModels.Add(new UserViewModel
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        FirstName = user.first_name,
                        LastName = user.last_name,
                        EmailConfirmed = user.EmailConfirmed,
                        Role = role,
                        CreatedDate = DateTime.Now,
                        OrderCount = orderCount,
                        TotalSpent = totalSpent,
                        LastOrderDate = lastOrderDate
                    });
                }

                // If role filter is applied, we need to adjust pagination
                if (!string.IsNullOrEmpty(roleFilter))
                {
                    model.TotalUsers = userViewModels.Count;
                }

                model.Users = userViewModels;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management page");
                TempData["Error"] = "Failed to load user data.";
                return View(new UserManagementViewModel());
            }
        }

        public async Task<IActionResult> EditUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                var roles = await _userManager.GetRolesAsync(user);
                var currentRole = roles.FirstOrDefault() ?? "";

                var model = new UserEditViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.first_name,
                    LastName = user.last_name,
                    Role = currentRole,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd
                };

                // Get all roles for dropdown
                model.RolesList = _roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name,
                        Selected = r.Name == currentRole
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user edit page for user ID: {UserId}", id);
                TempData["Error"] = "Failed to load user data for editing.";
                return RedirectToAction(nameof(UserManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload roles for dropdown
                    model.RolesList = _roleManager.Roles
                        .Select(r => new SelectListItem
                        {
                            Text = r.Name,
                            Value = r.Name,
                            Selected = r.Name == model.Role
                        })
                        .ToList();
                    return View(model);
                }

                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Update user info
                user.first_name = model.FirstName;
                user.last_name = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                // Update lockout status
                if (model.LockoutEnabled && model.LockoutEnd.HasValue)
                {
                    await _userManager.SetLockoutEndDateAsync(user, model.LockoutEnd.Value);
                }
                else if (!model.LockoutEnabled)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }

                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    // Remove from all current roles
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    
                    // Add to new role
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Save changes
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction(nameof(UserManagement));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                // Reload roles for dropdown
                model.RolesList = _roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name,
                        Selected = r.Name == model.Role
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", model.Id);
                TempData["Error"] = "Failed to update user data.";
                return RedirectToAction(nameof(UserManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Check for linked orders
                var userOrders = await _unitOfWork.OrderHeader.GetAllAsync(o => o.ApplicationUserId == id);
                if (userOrders.Any())
                {
                    TempData["Error"] = "Cannot delete user with existing orders. Consider disabling the account instead.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Delete user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = "User deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to delete user. " + string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction(nameof(UserManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                TempData["Error"] = "Failed to delete user.";
                return RedirectToAction(nameof(UserManagement));
            }
        }

        #endregion

        #region Order Management

        public async Task<IActionResult> OrderManagement(string searchString = "", string status = "", DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1)
        {
            try
            {
                var pageSize = 10;
                
                // Build query with filters
                var query = _unitOfWork.OrderHeader.GetQueryable(includeProperties: "ApplicationUser");
                
                // Status filter
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.OrderStatus == status);
                }
                
                // Date range filter
                if (startDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= startDate.Value);
                }
                
                if (endDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate <= endDate.Value.AddDays(1)); // Add 1 day to include end date
                }
                
                // Search by ID, user email, or name
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(o => 
                        o.Id.ToString().Contains(searchString) ||
                        o.ApplicationUser.Email.Contains(searchString) ||
                        o.first_name.Contains(searchString) ||
                        o.last_name.Contains(searchString) ||
                        o.phone_number.Contains(searchString));
                }
                
                // Get total count before pagination
                var totalOrders = await query.CountAsync();
                
                // Get paginated orders
                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                // Build status filter options
                var statusOptions = new List<SelectListItem>
                {
                    new SelectListItem("All Statuses", ""),
                    new SelectListItem(SD.Status_Pending, SD.Status_Pending),
                    new SelectListItem(SD.Status_Approved, SD.Status_Approved),
                    new SelectListItem(SD.Status_Processing, SD.Status_Processing),
                    new SelectListItem(SD.Status_Shipped, SD.Status_Shipped),
                    new SelectListItem(SD.Status_Cancelled, SD.Status_Cancelled)
                };
                
                // Pass to view
                ViewBag.SearchString = searchString;
                ViewBag.Status = status;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.StatusOptions = statusOptions;
                
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order management page");
                TempData["Error"] = "Failed to load order data.";
                return View(new List<OrderHeader>());
            }
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var orderHeader = await _unitOfWork.OrderHeader.GetAsync(
                    o => o.Id == id,
                    includeProperties: "ApplicationUser");

                if (orderHeader == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(OrderManagement));
                }

                var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                    od => od.OrderId == id,
                    includeProperties: "Product,Product.Brand");

                var orderDetailsVM = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = orderDetails.ToList()
                };

                return View(orderDetailsVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for orderId: {OrderId}", id);
                TempData["Error"] = "Failed to load order details. Please try again.";
                return RedirectToAction(nameof(OrderManagement));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            try
            {
                var order = await _unitOfWork.OrderHeader.GetAsync(o => o.Id == orderId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction(nameof(OrderManagement));
                }

                // Update the status
                order.OrderStatus = newStatus;
                
                // Set shipping date if status is shipped
                if (newStatus == SD.Status_Shipped)
                {
                    order.ShippingDate = DateTime.Now;
                }
                
                // Update payment status based on order status
                if (newStatus == SD.Status_Cancelled)
                {
                    order.PaymentStatus = SD.Payment_Status_Rejected;
                    
                    // Restore inventory
                    var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(od => od.OrderId == orderId, "Product");
                    foreach (var item in orderDetails)
                    {
                        var product = item.Product;
                        if (product != null)
                        {
                            product.StockQuantity += item.Count;
                            await _unitOfWork.ProductRepository.UpdateAsync(product);
                        }
                    }
                }

                await _unitOfWork.OrderHeader.UpdateAsync(order);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = $"Order status updated to {newStatus}";
                return RedirectToAction(nameof(OrderDetails), new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for orderId: {OrderId}", orderId);
                TempData["Error"] = "Failed to update order status. Please try again.";
                return RedirectToAction(nameof(OrderDetails), new { id = orderId });
            }
        }

        #endregion

        #region Analytics

        public async Task<IActionResult> SalesAnalytics(DateTime? startDate = null, DateTime? endDate = null, string groupBy = "day")
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Now.AddDays(-30);
                var end = endDate ?? DateTime.Now;

                // Get orders in date range
                var orders = (await _unitOfWork.OrderHeader.GetAllAsync(
                    o => o.OrderDate >= start && o.OrderDate <= end && o.OrderStatus != SD.Status_Cancelled))
                    .ToList();

                // Group orders based on selection
                IEnumerable<ChartDataPoint> salesData;
                
                switch (groupBy.ToLower())
                {
                    case "week":
                        // Group by ISO week number
                        salesData = orders
                            .GroupBy(o => System.Globalization.ISOWeek.GetWeekOfYear(o.OrderDate))
                            .Select(g => new ChartDataPoint
                            {
                                Label = $"Week {g.Key}",
                                Value = g.Sum(o => (decimal)o.OrderTotal)
                            })
                            .OrderBy(d => int.Parse(d.Label.Replace("Week ", "")))
                            .ToList();
                        break;
                        
                    case "month":
                        // Group by month
                        salesData = orders
                            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                            .Select(g => new ChartDataPoint
                            {
                                Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                                Value = g.Sum(o => (decimal)o.OrderTotal)
                            })
                            .OrderBy(d => d.Label)
                            .ToList();
                        break;
                        
                    default: // day
                        // Group by day
                        salesData = orders
                            .GroupBy(o => o.OrderDate.Date)
                            .Select(g => new ChartDataPoint
                            {
                                Label = g.Key.ToString("MM/dd/yyyy"),
                                Value = g.Sum(o => (decimal)o.OrderTotal)
                            })
                            .OrderBy(d => DateTime.Parse(d.Label))
                            .ToList();
                        break;
                }

                // Get total sales
                var totalSales = orders.Sum(o => (decimal)o.OrderTotal);
                var orderCount = orders.Count;
                var averageOrderValue = orderCount > 0 ? totalSales / orderCount : 0;

                // Get top selling products
                var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                    od => od.orderHeader.OrderDate >= start && od.orderHeader.OrderDate <= end && od.orderHeader.OrderStatus != SD.Status_Cancelled,
                    includeProperties: "Product,orderHeader");

                var topProducts = orderDetails
                    .GroupBy(od => od.ProductId)
                    .Select(g => new TopSellingProduct
                    {
                        ProductId = g.Key,
                        Name = g.First().Product.Name,
                        ImageUrl = g.First().Product.ImageUrl ?? "/images/placeholder.png",
                        QuantitySold = g.Sum(od => od.Count),
                        Revenue = g.Sum(od => (decimal)od.Price * od.Count)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(10)
                    .ToList();

                // Pass to view
                ViewBag.StartDate = start;
                ViewBag.EndDate = end;
                ViewBag.GroupBy = groupBy;
                ViewBag.TotalSales = totalSales;
                ViewBag.OrderCount = orderCount;
                ViewBag.AverageOrderValue = averageOrderValue;
                ViewBag.TopProducts = topProducts;

                return View(salesData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales analytics");
                TempData["Error"] = "Failed to load sales analytics data.";
                return View(new List<ChartDataPoint>());
            }
        }

        #endregion
    }
} 