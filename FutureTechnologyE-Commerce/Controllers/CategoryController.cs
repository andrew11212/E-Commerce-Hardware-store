using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;
using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FutureTechnologyE_Commerce.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = SD.Role_Admin)]
	public class CategoryController : Controller
	{
		private readonly IUnitOfWork unitOfWork;

		public CategoryController(IUnitOfWork unitOfWork)
		{
			this.unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			var category = unitOfWork.CategoryRepository.GetAll().ToList();
			return View(category);
		}
		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}
		[HttpPost]
		public IActionResult Create(Category category)
		{
			
			if (ModelState.IsValid)
			{
				unitOfWork.CategoryRepository.Add(category);
				unitOfWork.Save();
				TempData["Success"] = "Category updated successfully";
				return RedirectToAction(nameof(Index));  // Redirect back to the list after saving
			}
			return View(category); // Return view with model if validation fails
		}
		[HttpGet]
		public IActionResult Edit(int id)
		{

			var category = unitOfWork.CategoryRepository.Get(e => e.CategoryID == id);
			if (category == null)
			{
				return NotFound();
			}

			return View(category); // Return the view with the category data
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public IActionResult Edit(Category category)
		{
			if (ModelState.IsValid)
			{

				var categoryIndb = unitOfWork.CategoryRepository.Get(c => c.CategoryID == category.CategoryID);
				// or unitOfWork.Update(category); 
				if (categoryIndb != null)
				{
					categoryIndb.Name = category.Name;
					unitOfWork.Save();
					TempData["Success"] = "Category updated successfully";
					return RedirectToAction(nameof(Index));
				}
				return NotFound();
			}
			return View(category);
		}

		public IActionResult Delete(int id)
		{
			var category = unitOfWork.CategoryRepository.Get(c => c.CategoryID == id);
			if (category == null)
			{
				return NotFound(); // Return 404 if the category is not found
			}

			return View(category); // Return the confirmation view
		}
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public IActionResult DeleteConfirmed(int id)
		{
			var CategoryIndb = unitOfWork.CategoryRepository.Get(c => c.CategoryID == id);
			if (CategoryIndb != null)
			{
				unitOfWork.CategoryRepository.Remove(CategoryIndb);
				unitOfWork.Save();
				TempData["Success"] = "Category Deleted successfully";
				return RedirectToAction(nameof(Index));
			}
			return NotFound();
		}
	}
}
