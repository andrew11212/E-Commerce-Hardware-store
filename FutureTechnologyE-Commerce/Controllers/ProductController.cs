using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Ubsert(int? id)
        {
            ProductViewModel productVM = new()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.CategoryRepository.GetAll().Select(c =>
                    new SelectListItem { Text = c.Name, Value = c.CategoryID.ToString() }).ToList(),
                BrandList = _unitOfWork.BrandRepository.GetAll().Select(b =>
                    new SelectListItem { Text = b.Name, Value = b.BrandID.ToString() }).ToList(),
                ProductTypeList = _unitOfWork.ProductTypeRepository.GetAll().Select(pt =>
                    new SelectListItem { Text = pt.Name, Value = pt.ProductTypeID.ToString() }).ToList()
            };

            if (id == null || id == 0)
            {
                ViewBag.Title = "Create Product";
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.ProductRepository
                    .Get(p => p.ProductID == id,"Category","Brand","ProductType");

                if (productVM.Product == null)
                {
                    return NotFound();
                }

                ViewBag.Title = "Update Product";
                return View(productVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ubsert(ProductViewModel productVM, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns if validation fails
                productVM.CategoryList = _unitOfWork.CategoryRepository.GetAll()
                    .Select(c => new SelectListItem(c.Name, c.CategoryID.ToString())).ToList();
                productVM.BrandList = _unitOfWork.BrandRepository.GetAll()
                    .Select(b => new SelectListItem(b.Name, b.BrandID.ToString())).ToList();
                productVM.ProductTypeList = _unitOfWork.ProductTypeRepository.GetAll()
                    .Select(pt => new SelectListItem(pt.Name, pt.ProductTypeID.ToString())).ToList();

                return View(productVM);
            }

            string wwwRootPath = _webHostEnvironment.WebRootPath;

            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string productPath = Path.Combine(wwwRootPath, "images", "products");

                if (!Directory.Exists(productPath))
                {
                    Directory.CreateDirectory(productPath);
                }

                if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                {
                    var oldImage = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImage))
                    {
                        System.IO.File.Delete(oldImage);
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productVM.Product.ImageUrl = $"/images/products/{fileName}";
            }

            if (productVM.Product.ProductID == 0)
            {
                _unitOfWork.ProductRepository.Add(productVM.Product);
                TempData["success"] = "Product created successfully";
            }
            else
            {
                _unitOfWork.ProductRepository.Ubdate(productVM.Product);
                TempData["success"] = "Product updated successfully";
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var products = _unitOfWork.ProductRepository.GetAll(
                    includeProperties: "Category,Brand,ProductType"
                ).Select(p => new
                {
                    productID = p.ProductID,
                    name = p.Name,
                    description = p.Description,
                    price = p.Price,
                    categoryName = p.Category?.Name ?? "N/A", // Handle nulls
                    brandName = p.Brand?.Name ?? "N/A",
                    productTypeName = p.ProductType?.Name ?? "N/A",
                    stockQuantity = p.StockQuantity
                }).ToList();

                return Json(new { data = products });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving data", details = ex.Message });
            }
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var product = _unitOfWork.ProductRepository.Get(p => p.ProductID == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.ProductRepository.Remove(product);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Product deleted successfully" });
        }
        #endregion
    }
}