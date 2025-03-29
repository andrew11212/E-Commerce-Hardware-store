using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Models.ViewModels;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FutureTechnologyE_Commerce.Controllers
{
	//[Authorize(Roles = SD.Role_Admin)]

	public class ProductController : Controller
	{
		private readonly IUnitOfWork unitOfWork;
		private readonly IWebHostEnvironment webHostEnvironment;

		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			this.unitOfWork = unitOfWork;
			this.webHostEnvironment = webHostEnvironment;
		}


		public IActionResult Index()
		{
			var productList = unitOfWork.ProductRepository.GetAll(default, "Category");
			return View(productList);
		}

		[HttpGet]
		public IActionResult Ubsert(int? id)
		{
			// Initialize ProductViewModel
			ProductViewModel productVM = new ProductViewModel()
			{
				product = new Product(),
				CategoryList = unitOfWork.CategoryRepository.GetAll().Select(c => new SelectListItem
				{
					Text = c.Name,
					Value = c.CategoryID.ToString()
				})
			};

			// Set ViewBag.Title based on the action (Create or Update)
			if (id == null || id == 0)
			{
				ViewBag.Title = "Create Product";
				return View(productVM);  // For Create
			}
			else
			{
				ViewBag.Title = "Update Product";
				productVM.product = unitOfWork.ProductRepository.Get(p => p.ProductID == id.GetValueOrDefault());
				if (productVM.product == null)
				{
					return NotFound();
				}
				return View(productVM);  // For Update
			}
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Ubsert(ProductViewModel productVM, IFormFile? file)
		{
			if (ModelState.IsValid)
			{
				string wwwRootPath = webHostEnvironment.WebRootPath;

				if (file != null)
				{
					string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string productPath = Path.Combine(wwwRootPath, "images", "product"); // Lowercase directory

					// Create directory if not exists
					if (!Directory.Exists(productPath))
					{
						Directory.CreateDirectory(productPath);
					}

					// Save the file
					using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
					{
						file.CopyTo(fileStream);
					}

					productVM.product.ImageUrl = $"/images/product/{fileName}";
				}


				if (productVM.product.ProductID == 0)
				{
					unitOfWork.ProductRepository.Add(productVM.product);
					TempData["Success"] = "Product Created Successfully";
				}
				else
				{
					unitOfWork.ProductRepository.Ubdate(productVM.product);
					TempData["Success"] = "Product Updated Successfully";
				}

				unitOfWork.Save();

				return RedirectToAction("Index");
			}

			return View(productVM);
		}

		[HttpGet]
		public IActionResult GetAll()
		{
			try
			{
				var products = unitOfWork.ProductRepository.GetAll(
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
			var product = unitOfWork.ProductRepository.Get(p => p.ProductID == id);
			if (product == null)
			{
				return Json(new { success = false, message = "Error Deleting Product" });
			}
			var oldImage = Path.Combine(webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));
			if (System.IO.File.Exists(oldImage))
			{
				System.IO.File.Delete(oldImage);
			}
			unitOfWork.ProductRepository.Remove(product);
			unitOfWork.Save();
			return Json(new { success = true, message = "Deleted Successfully" });
		}
	}
}