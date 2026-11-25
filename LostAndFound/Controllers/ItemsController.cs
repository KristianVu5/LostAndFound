using System.Globalization;
using System.Security.Claims;
using LostAndFound.Data;
using LostAndFound.Models;
using LostAndFound.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace LostAndFound.Controllers
{
    public class ItemsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ItemsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] string? location,
            [FromQuery] int? categoryId,
            [FromQuery] DateOnly? dateFrom,
            [FromQuery] DateOnly? dateTo,
            [FromQuery] ItemType? type,
            [FromQuery] int page = 1)
        {
            const int pageSize = 10;

            if (page < 1)
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

            if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
            {
                ModelState.AddModelError(
                    nameof(dateTo),
                    "Началната дата не може да е след крайната дата.");
            }

            await LoadCategoriesAsync(categoryId);

            if (!ModelState.IsValid)
            {
                return View(Array.Empty<Report>());
            }

            var query = _context.Reports.AsNoTracking();

            if (!string.IsNullOrEmpty(searchLocation))
            {
                query = query.Where(report => report.Location.Contains(searchLocation));
            }

            if (categoryId is int selectedCategoryId)
            {
                query = query.Where(report => report.Item.CategoryId == selectedCategoryId);
            }

            if (type is ItemType selectedType)
            {
                query = query.Where(report => report.Type == selectedType);
            }

            if (dateFrom is DateOnly startDate)
            {
                query = query.Where(report => report.OccurredOn >= startDate);
            }

            if (dateTo is DateOnly endDate)
            {
                query = query.Where(report => report.OccurredOn <= endDate);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0)
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

            var reports = await query
                .Include(report => report.Item)
                    .ThenInclude(item => item.Category)
                .OrderByDescending(report => report.OccurredOn)
                .ThenByDescending(report => report.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArrayAsync();

            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var report = await _context.Reports
                .AsNoTracking()
                .Include(report => report.Item)
                    .ThenInclude(item => item.Category)
                .Include(report => report.Item)
                    .ThenInclude(item => item.Photos)
                .FirstOrDefaultAsync(report => report.Id == id);

            if (report is null)
            {
                return NotFound();
            }

            var oppositeType = report.Type == ItemType.Lost
                ? ItemType.Found
                : ItemType.Lost;
            var earliestDate = report.OccurredOn.AddDays(-30);
            var latestDate = report.OccurredOn.AddDays(30);

            var candidates = await _context.Reports
                .AsNoTracking()
                .Include(candidate => candidate.Item)
                    .ThenInclude(item => item.Category)
                .Where(candidate =>
                    candidate.Id != report.Id
                    && candidate.Type == oppositeType
                    && candidate.Item.CategoryId == report.Item.CategoryId
                    && candidate.OccurredOn >= earliestDate
                    && candidate.OccurredOn <= latestDate)
                .ToListAsync();

            var potentialMatches = candidates
                .OrderBy(candidate =>
                    Math.Abs(candidate.OccurredOn.DayNumber - report.OccurredOn.DayNumber))
                .Take(5)
                .ToArray();

            return View(new ItemDetailsViewModel
            {
                Report = report,
                PotentialMatches = potentialMatches,
                CanManage = User.Identity?.IsAuthenticated == true && CanManageReport(report)
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View(new ItemForm());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemForm form)
        {
            var photoError = ValidatePhotos(form.PhotoFiles);

            if (photoError is not null)
            {
                ModelState.AddModelError(nameof(form.PhotoFiles), photoError);
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId is null)
                {
                    return Challenge();
                }

                var item = new Item
                {
                    Title = form.Title.Trim(),
                    Description = form.Description.Trim(),
                    CategoryId = form.CategoryId!.Value
                };

                var report = new Report
                {
                    Item = item,
                    Location = form.Location.Trim(),
                    OccurredOn = form.OccurredOn!.Value,
                    Type = form.Type!.Value,
                    CreatedById = userId
                };

                var savedPhotoPhysicalPaths = new List<string>();

                try
                {
                    foreach (var photoFile in form.PhotoFiles.Where(photo => photo.Length > 0))
                    {
                        var savedPhoto = await SavePhotoFileAsync(photoFile);
                        savedPhotoPhysicalPaths.Add(savedPhoto.PhysicalPath);

                        item.Photos.Add(new ItemPhoto
                        {
                            Item = item,
                            ImagePath = savedPhoto.RelativePath,
                            UploadedAt = DateTime.UtcNow
                        });
                    }

                    // Save the report, its item, and the item's photos as one EF Core object graph.
                    _context.Reports.Add(report);
                    await _context.SaveChangesAsync();
                    TempData["ItemCreated"] = "Обявата е създадена успешно.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqlException { Number: 547 })
                {
                    foreach (var photoPath in savedPhotoPhysicalPaths)
                    {
                        DeletePhotoFile(photoPath);
                    }

                    ModelState.AddModelError(
                        nameof(ItemForm.CategoryId),
                        "Избраната категория вече не съществува. Избери друга.");
                }
                catch
                {
                    foreach (var photoPath in savedPhotoPhysicalPaths)
                    {
                        DeletePhotoFile(photoPath);
                    }

                    throw;
                }
            }

            await LoadCategoriesAsync(form.CategoryId);
            return View(form);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var report = await _context.Reports
                .Include(report => report.Item)
                .FirstOrDefaultAsync(report => report.Id == id);

            if (report is null)
            {
                return NotFound();
            }

            if (!CanManageReport(report))
            {
                return Forbid();
            }

            ViewData["ReportId"] = id;
            await LoadCategoriesAsync(report.Item.CategoryId);

            return View(new ItemForm
            {
                Title = report.Item.Title,
                Description = report.Item.Description,
                Location = report.Location,
                OccurredOn = report.OccurredOn,
                Type = report.Type,
                CategoryId = report.Item.CategoryId
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, ItemForm form)
        {
            var report = await _context.Reports
                .Include(report => report.Item)
                .FirstOrDefaultAsync(report => report.Id == id);

            if (report is null)
            {
                return NotFound();
            }

            if (!CanManageReport(report))
            {
                return Forbid();
            }

            var photoError = ValidatePhotos(form.PhotoFiles);

            if (photoError is not null)
            {
                ModelState.AddModelError(nameof(form.PhotoFiles), photoError);
            }

            if (ModelState.IsValid)
            {
                report.Item.Title = form.Title.Trim();
                report.Item.Description = form.Description.Trim();
                report.Item.CategoryId = form.CategoryId!.Value;
                report.Location = form.Location.Trim();
                report.OccurredOn = form.OccurredOn!.Value;
                report.Type = form.Type!.Value;

                var savedPhotoPhysicalPaths = new List<string>();

                try
                {
                    foreach (var photoFile in form.PhotoFiles.Where(photo => photo.Length > 0))
                    {
                        var savedPhoto = await SavePhotoFileAsync(photoFile);
                        savedPhotoPhysicalPaths.Add(savedPhoto.PhysicalPath);

                        _context.ItemPhotos.Add(new ItemPhoto
                        {
                            ItemId = report.Item.Id,
                            ImagePath = savedPhoto.RelativePath,
                            UploadedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    foreach (var photoPath in savedPhotoPhysicalPaths)
                    {
                        DeletePhotoFile(photoPath);
                    }

                    return NotFound();
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqlException { Number: 547 })
                {
                    foreach (var photoPath in savedPhotoPhysicalPaths)
                    {
                        DeletePhotoFile(photoPath);
                    }

                    ModelState.AddModelError(
                        nameof(ItemForm.CategoryId),
                        "Избраната категория вече не съществува. Избери друга.");
                }
                catch
                {
                    foreach (var photoPath in savedPhotoPhysicalPaths)
                    {
                        DeletePhotoFile(photoPath);
                    }

                    throw;
                }
            }

            ViewData["ReportId"] = id;
            await LoadCategoriesAsync(form.CategoryId);
            return View(form);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var report = await _context.Reports
                .Include(report => report.Item)
                .FirstOrDefaultAsync(report => report.Id == id);

            if (report is null)
            {
                return NotFound();
            }

            if (!CanManageReport(report))
            {
                return Forbid();
            }

            return View(report);
        }

        [Authorize]
        [HttpPost]
        [ActionName(nameof(Delete))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
        {
            var report = await _context.Reports
                .Include(report => report.Item)
                    .ThenInclude(item => item.Photos)
                .FirstOrDefaultAsync(report => report.Id == id);

            if (report is null)
            {
                return NotFound();
            }

            if (!CanManageReport(report))
            {
                return Forbid();
            }

            var item = report.Item;
            var hasOtherReports = await _context.Reports
                .AnyAsync(other => other.ItemId == report.ItemId && other.Id != report.Id);
            var photoPathsToDelete = hasOtherReports
                ? Array.Empty<string>()
                : item.Photos
                    .Select(photo => GetPhotoPhysicalPath(photo.ImagePath))
                    .ToArray();

            _context.Reports.Remove(report);

            if (!hasOtherReports)
            {
                _context.Items.Remove(item);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }

            foreach (var photoPath in photoPathsToDelete)
            {
                DeletePhotoFile(photoPath);
            }
            TempData["ItemDeleted"] = "Обявата е изтрита.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(string RelativePath, string PhysicalPath)> SavePhotoFileAsync(
    IFormFile photo)
        {
            var uploadDirectory = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "uploads",
                "item-photos");

            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadDirectory, fileName);

            try
            {
                await using var fileStream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew);

                await photo.CopyToAsync(fileStream);
            }
            catch
            {
                DeletePhotoFile(physicalPath);
                throw;
            }

            var relativePath = $"/uploads/item-photos/{fileName}";

            return (relativePath, physicalPath);
        }

        private static void DeletePhotoFile(string? physicalPath)
        {
            if (!string.IsNullOrWhiteSpace(physicalPath)
                && System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        private string GetPhotoPhysicalPath(string imagePath)
        {
            var fileName = Path.GetFileName(imagePath);

            return Path.Combine(
                _webHostEnvironment.WebRootPath,
                "uploads",
                "item-photos",
                fileName);
        }

        private static string? ValidatePhotos(ICollection<IFormFile>? photos)
        {
            if (photos is null)
            {
                return null;
            }

            var selectedPhotos = photos
                .Where(photo => photo.Length > 0)
                .ToArray();

            if (selectedPhotos.Length > 5)
            {
                return "Можеш да качиш най-много 5 снимки към една вещ.";
            }

            const long maxSizeBytes = 5 * 1024 * 1024;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            foreach (var photo in selectedPhotos)
            {
                if (photo.Length > maxSizeBytes)
                {
                    return "Всяка снимка трябва да е до 5 MB.";
                }

                var extension = Path.GetExtension(photo.FileName);
                var isAllowed = allowedExtensions.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase);

                if (!isAllowed)
                {
                    return "Разрешени са само снимки във формат JPG или PNG.";
                }
            }

            return null;
        }
        private bool CanManageReport(Report report)
        {
            return User.IsInRole("Admin")
                || report.CreatedById == User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories,
                nameof(Category.Id),
                nameof(Category.Name),
                selectedCategoryId);
        }
    }
}
