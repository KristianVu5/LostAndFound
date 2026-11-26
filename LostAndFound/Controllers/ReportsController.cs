using LostAndFound.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostAndFound.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using LostAndFound.ViewModels;

namespace LostAndFound.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ByPeriod(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if(dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
            {
                ModelState.AddModelError(nameof(dateTo),
                    "Крайната дата трябва да е равна на или след началната дата.");
            }

            if (!ModelState.IsValid)
            {
                return View(Array.Empty<LostAndFound.Models.Report>());
            }

            var query = _context.Reports.AsNoTracking();

            if(dateFrom is DateOnly startDate)
            {
                query = query.Where(report => report.OccurredOn >= startDate);
            }

            if(dateTo is DateOnly endDate)
            {
                query = query.Where(report => report.OccurredOn <= endDate);
            }

            var reports = await query
                .Include(report => report.Item)
                .OrderByDescending(report => report.OccurredOn)
                .ThenByDescending(report => report.Id)
                .ToListAsync();

            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> LostByCategory(int? categoryId)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();

            ViewData["Categories"] = new SelectList(
                categories, nameof(Category.Id), nameof(Category.Name), categoryId);

            if(categoryId.HasValue && !categories.Any(category => category.Id == categoryId))
            {
                ModelState.AddModelError(nameof(categoryId), "Избери валидна категория.");
            }

            if (!ModelState.IsValid)
            {
                return View(Array.Empty<Report>());
            }

            var query = _context.Reports
                 .AsNoTracking()
                 .Where(report => report.Type == ItemType.Lost);

            if (categoryId is int selectedCategoryId)
            {
                query = query.Where(report => report.Item.CategoryId == selectedCategoryId);
            }

            var reports = await query
                .Include(report => report.Item)
                .ThenInclude(item => item.Category)
                .OrderByDescending(report => report.OccurredOn)
                .ThenByDescending(report => report.Id)
                .ToListAsync();

            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> FoundByLocation(string? location)
        {
            var searchLocation = location?.Trim();
            ViewData["Location"] = searchLocation;

            var query = _context.Reports
                .AsNoTracking()
                .Where(report => report.Type == ItemType.Found);

            if (!string.IsNullOrEmpty(searchLocation))
            {
                query = query.Where(report => report.Location.Contains(searchLocation));
            }

            var reports = await query
                .Include(report => report.Item)
                .OrderByDescending(report => report.OccurredOn)
                .ThenByDescending(report => report.Id)
                .ToListAsync();

            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> CategoryPopularity()
        {
            var rows = await _context.Reports
                .GroupBy(report => new
                {
                    report.Item.CategoryId,
                    CategoryName = report.Item.Category.Name
                })
                .Select(group => new CategoryPopularityRow
                {
                    CategoryName = group.Key.CategoryName,
                    ReportCount = group.Count()
                })
                .OrderByDescending(row => row.ReportCount)
                .ThenBy(row => row.CategoryName)
                .ToListAsync();

            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> PotentialMatches()
        {
            const int matchWindowDays = 30;

            var foundReports = _context.Reports
                .Where(report => report.Type == ItemType.Found);

            var rows = await _context.Reports
                .Where(lost => lost.Type == ItemType.Lost)
                .Select(lost => new PotentialMatchRow
                {
                    ReportId = lost.Id,
                    Title = lost.Item.Title,
                    CategoryName = lost.Item.Category.Name,
                    OccurredOn = lost.OccurredOn,
                    MatchCount = foundReports.Count(found =>
                        found.Item.CategoryId == lost.Item.CategoryId &&
                        Math.Abs(EF.Functions.DateDiffDay(lost.OccurredOn, found.OccurredOn))
                            <= matchWindowDays)
                })
                .Where(row => row.MatchCount > 0)
                .OrderByDescending(row => row.MatchCount)
                .ThenByDescending(row => row.OccurredOn)
                .ThenByDescending(row => row.ReportId)
                .ToListAsync();

            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var counts = await _context.Reports
                .GroupBy(report => report.Type)
                .Select(group => new
                {
                    Type = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(row => row.Type, row => row.Count);

            var model = new ReportStatisticsViewModel
            {
                LostCount = counts.GetValueOrDefault(ItemType.Lost),
                FoundCount = counts.GetValueOrDefault(ItemType.Found)
            };

            return View(model);
        }
    }
}
