using System.Collections.Generic;
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

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ItemPhoto> Photos { get; set; } = new List<ItemPhoto>();
    }
}
