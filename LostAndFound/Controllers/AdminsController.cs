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

        public AdminsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Promote()
        {
            return View(new PromoteAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(PromoteAdminViewModel model)
        {
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
                    "Неуспешно назначаване на администраторската роля.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Потребителят {user.Email} вече е администратор.";

            return RedirectToAction(nameof(Promote));
        }
    }
}
