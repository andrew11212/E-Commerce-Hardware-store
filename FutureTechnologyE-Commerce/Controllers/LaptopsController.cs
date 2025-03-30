using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Utility;
using FutureTechnologyE_Commerce.Repository.IRepository;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Authorize(Roles = SD.Role_Admin)]
	public class LaptopsController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public LaptopsController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
		}

		// GET: Laptops
		public IActionResult Index()
		{
			var laptops = _unitOfWork.LaptopRepository.GetAll(includeProperties: "Category,Brand,ProductType");
			return View(laptops);
		}

		// GET: Laptops/Details/5
		public IActionResult Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id, includeProperties: "Brand,Category,ProductType");
			if (laptop == null)
			{
				return NotFound();
			}

			return View(laptop);
		}

		// GET: Laptops/Create
		public IActionResult Create()
		{
			ViewData["BrandID"] = new SelectList(_unitOfWork.BrandRepository.GetAll(), "BrandID", "Name");
			ViewData["CategoryID"] = new SelectList(_unitOfWork.CategoryRepository.GetAll(), "CategoryID", "Name");
			ViewData["ProductTypeID"] = new SelectList(_unitOfWork.ProductTypeRepository.GetAll(), "ProductTypeID", "Name");
			return View();
		}

		// POST: Laptops/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("Processor,RAM,Storage,ScreenSize,GraphicsCard,Name,Description,Price,CategoryID,BrandID,StockQuantity,ProductTypeID")] Laptop laptop, IFormFile? file)
		{
			if (ModelState.IsValid)
			{
				// Handle image upload
				if (file != null && file.Length > 0)
				{
					string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

					using (var fileStream = new FileStream(imagePath, FileMode.Create))
					{
						await file.CopyToAsync(fileStream);
					}

					laptop.ImageUrl = "/images/products/" + fileName; // Store the relative path
				}

				_unitOfWork.LaptopRepository.Add(laptop);
				_unitOfWork.Save(); // Save changes

				return RedirectToAction(nameof(Index));
			}

			// If ModelState is not valid, repopulate dropdowns
			ViewData["BrandID"] = new SelectList(_unitOfWork.BrandRepository.GetAll(), "BrandID", "Name", laptop.BrandID);
			ViewData["CategoryID"] = new SelectList(_unitOfWork.CategoryRepository.GetAll(), "CategoryID", "Name", laptop.CategoryID);
			ViewData["ProductTypeID"] = new SelectList(_unitOfWork.ProductTypeRepository.GetAll(), "ProductTypeID", "Name", laptop.ProductTypeID);
			return View(laptop);
		}

		// GET: Laptops/Edit/5
		public IActionResult Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id);
			if (laptop == null)
			{
				return NotFound();
			}

			ViewData["BrandID"] = new SelectList(_unitOfWork.BrandRepository.GetAll(), "BrandID", "Name", laptop.BrandID);
			ViewData["CategoryID"] = new SelectList(_unitOfWork.CategoryRepository.GetAll(), "CategoryID", "Name", laptop.CategoryID);
			ViewData["ProductTypeID"] = new SelectList(_unitOfWork.ProductTypeRepository.GetAll(), "ProductTypeID", "Name", laptop.ProductTypeID);
			return View(laptop);
		}

		// POST: Laptops/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("Processor,RAM,Storage,ScreenSize,GraphicsCard,ProductID,Name,Description,Price,CategoryID,BrandID,StockQuantity,ProductTypeID,ImageUrl")] Laptop laptop, IFormFile file)
		{
			if (id != laptop.ProductID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				// Handle image upload
				if (file != null && file.Length > 0)
				{
					string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);

					// Delete the old image if it exists
					if (!string.IsNullOrEmpty(laptop.ImageUrl))
					{
						string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, laptop.ImageUrl.TrimStart('/')); //remove starting /
						if (System.IO.File.Exists(oldImagePath))
						{
							System.IO.File.Delete(oldImagePath);
						}
					}

					using (var fileStream = new FileStream(imagePath, FileMode.Create))
					{
						await file.CopyToAsync(fileStream);
					}

					laptop.ImageUrl = "/images/products/" + fileName; //update with new image
				}
				_unitOfWork.LaptopRepository.Update(laptop);
				_unitOfWork.Save();
				return RedirectToAction(nameof(Index));
			}
			//if model state is invalid
			ViewData["BrandID"] = new SelectList(_unitOfWork.BrandRepository.GetAll(), "BrandID", "Name", laptop.BrandID);
			ViewData["CategoryID"] = new SelectList(_unitOfWork.CategoryRepository.GetAll(), "CategoryID", "Name", laptop.CategoryID);
			ViewData["ProductTypeID"] = new SelectList(_unitOfWork.ProductTypeRepository.GetAll(), "ProductTypeID", "Name", laptop.ProductTypeID);
			return View(laptop);
		}

		// GET: Laptops/Delete/5
		public IActionResult Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id, includeProperties: "Brand,Category,ProductType");
			if (laptop == null)
			{
				return NotFound();
			}

			return View(laptop);
		}

		// POST: Laptops/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteConfirmed(int id)
		{
			var laptop = _unitOfWork.LaptopRepository.Get(l => l.ProductID == id);
			if (laptop != null)
			{
				// Delete the image file
				if (!string.IsNullOrEmpty(laptop.ImageUrl))
				{
					string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, laptop.ImageUrl.TrimStart('/'));
					if (System.IO.File.Exists(imagePath))
					{
						System.IO.File.Delete(imagePath);
					}
				}
				_unitOfWork.LaptopRepository.Remove(laptop);
				_unitOfWork.Save();
			}

			return RedirectToAction(nameof(Index));
		}
	}
}
