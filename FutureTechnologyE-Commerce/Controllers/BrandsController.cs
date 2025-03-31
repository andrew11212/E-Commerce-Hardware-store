using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FutureTechnologyE_Commerce.Data;
using FutureTechnologyE_Commerce.Models;
using FutureTechnologyE_Commerce.Repository.IRepository;

namespace FutureTechnologyE_Commerce.Controllers
{
	public class BrandsController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		public BrandsController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		// GET: Brands
		public async Task<IActionResult> Index()
		{
			var brands = await _unitOfWork.BrandRepository.GetAllAsync();
			return View(brands.ToList());
		}

		// GET: Brands/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var brand = await _unitOfWork.BrandRepository.GetAsync(m => m.BrandID == id);
			if (brand == null)
			{
				return NotFound();
			}

			return View(brand);
		}

		// GET: Brands/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: Brands/Create
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("BrandID,Name")] Brand brand)
		{
			if (ModelState.IsValid)
			{
				await _unitOfWork.BrandRepository.AddAsync(brand);
				await _unitOfWork.SaveAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(brand);
		}

		// GET: Brands/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var brand = await _unitOfWork.BrandRepository.GetAsync(m => m.BrandID == id);
			if (brand == null)
			{
				return NotFound();
			}
			return View(brand);
		}

		// POST: Brands/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("BrandID,Name")] Brand brand)
		{
			if (id != brand.BrandID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					await _unitOfWork.BrandRepository.UpdateAsync(brand);
					await _unitOfWork.SaveAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!await BrandExists(brand.BrandID))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			return View(brand);
		}

		// GET: Brands/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var brand = await _unitOfWork.BrandRepository.GetAsync(m => m.BrandID == id);
			if (brand == null)
			{
				return NotFound();
			}

			return View(brand);
		}

		// POST: Brands/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var brand = await _unitOfWork.BrandRepository.GetAsync(m => m.BrandID == id);
			if (brand != null)
			{
				await _unitOfWork.BrandRepository.RemoveAsync(brand);
			}

			await _unitOfWork.SaveAsync();
			return RedirectToAction(nameof(Index));
		}

		private async Task<bool> BrandExists(int id)
		{
			return await _unitOfWork.BrandRepository.GetAsync(e => e.BrandID == id) != null;
		}
	}
}