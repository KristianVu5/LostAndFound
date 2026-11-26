using System.ComponentModel.DataAnnotations;

namespace LostAndFound.ViewModels
{
    public class ItemClaimForm
    {
        [Required(ErrorMessage = "Моля, напишете кратко съобщение.")]
        [StringLength(1000, ErrorMessage = "Съобщението не може да е по-дълго от 1000 символа.")]
        public string Message { get; set; } = string.Empty;
    }
}