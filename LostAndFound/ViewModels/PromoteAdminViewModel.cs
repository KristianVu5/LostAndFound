using System.ComponentModel.DataAnnotations;

namespace LostAndFound.ViewModels
{
    public class PromoteAdminViewModel
    {
        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Въведи валиден имейл адрес.")]
        [Display(Name = "Имейл на регистрирания потребител")]
        public string Email { get; set; } = string.Empty;
    }
}
