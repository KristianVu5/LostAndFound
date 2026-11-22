using LostAndFound.Models;
using LostAndFound.ViewModels;
using LostAndFound.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace LostAndFound.Controllers
{
    public class CategoriesController : Controller
    {

        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryForm());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryForm form)
        {
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            var category = new Category
            {
                Name = form.Name.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute]int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if(category is null)
            {
                return NotFound();
            }

            ViewData["CategoryId"] = category.Id;
            return View(new CategoryForm { Name = category.Name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, CategoryForm form)
        {
            var category = await _context.Categories.FindAsync(id);

            if(category is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = id;
                return View(form);
            }

            category.Name = form.Name.Trim();
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if(category is null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ActionName(nameof(Delete))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
        {
            try
            {
                await _context.Categories
                    .Where(category => category.Id == id)
                    .ExecuteDeleteAsync();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["DeleteError"] =
                    "Категорията се използва в обяви и не може да бъде изтрита.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
