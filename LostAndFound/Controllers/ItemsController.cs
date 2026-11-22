using LostAndFound.Data;
using LostAndFound.Models;
using LostAndFound.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace LostAndFound.Controllers
{
    public class ItemsController : Controller
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .AsNoTracking()
                .Include(item => item.Category)
                .OrderByDescending(item => item.OccurredOn)
                .ThenByDescending(item => item.Id)
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var item = await _context.Items
                .AsNoTracking()
                .Include(item => item.Category)
                .FirstOrDefaultAsync(item => item.Id == id);

            if( item is null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View(new ItemForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemForm form)
        {
            if (ModelState.IsValid)
            {
                var item = new Item
                {
                    Title = form.Title.Trim(),
                    Description = form.Description.Trim(),
                    Location = form.Location.Trim(),
                    OccurredOn = form.OccurredOn!.Value,
                    Type = form.Type!.Value,
                    CategoryId = form.CategoryId!.Value
                };

                _context.Items.Add(item);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["ItemCreated"] = "Обявата е пбликувана.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqlException { Number: 547 })
                {
                    ModelState.AddModelError(nameof(ItemForm.CategoryId),
                        "Избраната категория вече не е налична. Избери друга.");
                }
            }

            await LoadCategoriesAsync();
            return View(form);
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories, nameof(Category.Id), nameof(Category.Name));
        }
    }
}
