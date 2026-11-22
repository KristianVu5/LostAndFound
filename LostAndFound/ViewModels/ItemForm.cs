using System.ComponentModel.DataAnnotations;
using LostAndFound.Models;

namespace LostAndFound.ViewModels
{
    public class ItemForm
    {
        [Required(ErrorMessage = "Въведи заглавие.")]
        [StringLength(120, ErrorMessage = "Заглавието може да съдържа до 120 символа.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Въведи описание.")]
        [StringLength(1000, ErrorMessage = "Описанието може да съдържа до 1000 символа.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Въведи място.")]
        [StringLength(150, ErrorMessage = "Мясото може да съдържа до 150 символа.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Избери дата.")]
        public DateOnly? OccurredOn { get; set; }

        [Required(ErrorMessage = "Избери тип на обявата.")]
        public ItemType? Type { get; set; }

        [Required(ErrorMessage = "Избери категория.")]
        public int? CategoryId { get; set; }

    }
}
