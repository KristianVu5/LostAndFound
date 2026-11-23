using LostAndFound.Data;
using LostAndFound.Models;
using LostAndFound.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using System.Globalization;

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
        public async Task<IActionResult> Index([FromQuery] string? location, 
                                                [FromQuery] int? categoryId,
                                                [FromQuery] DateOnly? dateFrom,
                                                [FromQuery] DateOnly? dateTo,
                                                [FromQuery] ItemType? type,
                                                [FromQuery] int page = 1)
        {
            const int pageSize = 10;
            if(page < 1)
            {
                page = 1;
            }

            var searchLocation = location?.Trim();
            ViewData["Location"] = searchLocation;
            ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewData["Type"] = type;
            ViewData["HasFilters"] = !string.IsNullOrEmpty(searchLocation)
                || categoryId.HasValue || dateFrom.HasValue || dateTo.HasValue || type.HasValue;

            if(dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
            {
                ModelState.AddModelError(nameof(dateTo),
                   "Крайната дата трябва да е на или след началната.");
            }

            await LoadCategoriesAsync(categoryId);

            if (!ModelState.IsValid)
            {
                return View(Array.Empty<Item>());
            }

            var query = _context.Items.AsNoTracking();

            if (!string.IsNullOrEmpty(searchLocation))
            {
                query = query.Where(item => item.Location.Contains(searchLocation));
            }

            if(categoryId is int selectedCategoryId)
            {
                query = query.Where(item => item.CategoryId == selectedCategoryId);
            }

            if(type is ItemType selectedType)
            {
                query = query.Where(item => item.Type == selectedType);
            }

            if(dateFrom is DateOnly startDate)
            {
                query = query.Where(item => item.OccurredOn >= startDate);
            }

            if(dateTo is DateOnly endDate)
            {
                query = query.Where(item => item.OccurredOn <= endDate);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if(totalPages == 0)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                page = totalPages;
            }

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["CategoryId"] = categoryId;

            var items = await query
                .Include(item => item.Category)
                .OrderByDescending(item => item.OccurredOn)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArrayAsync();

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

            var oppositeType = item.Type == ItemType.Lost
                ? ItemType.Found
                : ItemType.Lost;
            var earliesDate = item.OccurredOn.AddDays(-30);
            var latestDate = item.OccurredOn.AddDays(30);

            var candidates = await _context.Items
                .AsNoTracking()
                .Include(candidate => candidate.Category)
                .Where(candidate =>
                candidate.Id != item.Id
                && candidate.Type == oppositeType
                && candidate.CategoryId == item.CategoryId
                && candidate.OccurredOn >= earliesDate
                && candidate.OccurredOn <= latestDate)
                .ToListAsync();

            var potentialMatches = candidates
                .OrderBy(candidates =>
                Math.Abs(candidates.OccurredOn.DayNumber - item.OccurredOn.DayNumber))
                .Take(5)
                .ToArray();

            return View(new ItemDetailsViewModel
            {
                Item = item,
                PotentialMatches = potentialMatches
            });
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

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            ViewData["ItemId"] = id;
            await LoadCategoriesAsync();
            return View(new ItemForm
            {
                Title = item.Title,
                Description = item.Description,
                Location = item.Location,
                OccurredOn = item.OccurredOn,
                Type = item.Type,
                CategoryId = item.CategoryId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, ItemForm form)
        {
            var item = await _context.Items.FindAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                item.Title = form.Title.Trim();
                item.Description = form.Description.Trim();
                item.Location = form.Location.Trim();
                item.OccurredOn = form.OccurredOn!.Value;
                item.Type = form.Type!.Value;
                item.CategoryId = form.CategoryId!.Value;

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqlException { Number: 547 })
                {
                    ModelState.AddModelError(nameof(ItemForm.CategoryId),
                        "Избраната категория вече не е налична. Избери друга.");
                }
            }

            ViewData["ItemId"] = id;
            await LoadCategoriesAsync();
            return View(form);
        }

        [HttpGet]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var item = await _context.Items.FindAsync(id);

            if(item is null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName(nameof(Delete))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
        {
            var deletedCoutn = await _context.Items
                .Where(item => item.Id == id)
                .ExecuteDeleteAsync();

            if(deletedCoutn == 0)
            {
                return NotFound();
            }

            TempData["ItemDeleted"] = "Обявата е изтрита.";
            return RedirectToAction(nameof(Index));
        }

        

        private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories, nameof(Category.Id), nameof(Category.Name), selectedCategoryId);
        }
    }
}
