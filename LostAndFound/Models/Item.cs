using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models
{
    public class Item
    {
        public int Id { get; set; }
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        [MaxLength(150)]
        public string Location { get; set; } = string.Empty;
        public DateOnly OccurredOn { get; set; }
        public ItemType Type { get; set; }
        public int CategoryId { get; set; }
        public string? CreatedById { get; set; }
        public Category Category { get; set; } = null!;
    }
}
