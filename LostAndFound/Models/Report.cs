using System.ComponentModel.DataAnnotations;
using LostAndFound.Data;

namespace LostAndFound.Models
{
    public class Report
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        // Nullable so existing listings without an owner can be preserved.
        public string? CreatedById { get; set; }

        [Required]
        [MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        public DateOnly OccurredOn { get; set; }

        public ItemType Type { get; set; }

        public Item Item { get; set; } = null!;

        public ApplicationUser? CreatedBy { get; set; }

        public ICollection<ItemClaim> Claims { get; set; } = new List<ItemClaim>();
    }
}
