using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Models
{
    public class ItemPhoto
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImagePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }

        public Item Item { get; set; } = null!;
    }
}
