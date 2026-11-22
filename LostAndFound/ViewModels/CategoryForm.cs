using System.ComponentModel.DataAnnotations;

namespace LostAndFound.ViewModels
{
    public class CategoryForm
    {
        [Required(ErrorMessage = "Въведи име на категорията.")]
        [StringLength(100, ErrorMessage = "Името може да съдържа до 100 символа.")]
        public string Name { get; set; } = string.Empty;
    }
}
