using LostAndFound.Data;
using LostAndFound.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AdminsController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await IsConfiguredAdminAsync())
            {
                return Forbid();
            }

            var configuredAdminEmail = _configuration["AdminEmail"];
            var administrators = await _userManager.GetUsersInRoleAsync("Admin");

            var model = administrators
                .OrderBy(user => user.Email)
                .Select(user => new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "(без имейл)",
                    IsConfiguredAdmin = string.Equals(
                        user.Email,
                        configuredAdminEmail,
                        StringComparison.OrdinalIgnoreCase)
                })
                .ToArray();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Promote()
        {
            if (!await IsConfiguredAdminAsync())
            {
                return Forbid();
            }

            return View(new PromoteAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(PromoteAdminViewModel model)
        {
            if (!await IsConfiguredAdminAsync())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());

            if (user is null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Не е намерен регистриран потребител с този имейл.");

                return View(model);
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Този потребител вече е администратор.");

                return View(model);
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Неуспешно добавяне на потребителя като администратор.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Потребителят {user.Email} вече е администратор.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Demote([FromRoute] string id)
        {
            if (!await IsConfiguredAdminAsync())
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            var configuredAdminEmail = _configuration["AdminEmail"];
            if (string.Equals(
                user.Email,
                configuredAdminEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Основният администратор не може да бъде премахнат от ролята си.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["ErrorMessage"] = "Този потребител вече не е администратор.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var userRoleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!userRoleResult.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        "Неуспешно връщане на потребителя към стандартната роля.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Неуспешно премахване на администраторската роля.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                $"{user.Email} вече е обикновен потребител.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsConfiguredAdminAsync()
        {
            var configuredAdminEmail = _configuration["AdminEmail"];
            if (string.IsNullOrWhiteSpace(configuredAdminEmail))
            {
                return false;
            }

            var currentUser = await _userManager.GetUserAsync(User);

            return currentUser is not null
                && string.Equals(
                    currentUser.Email,
                    configuredAdminEmail,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
