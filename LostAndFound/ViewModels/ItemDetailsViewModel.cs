using LostAndFound.Models;

namespace LostAndFound.ViewModels
{
    public class ItemDetailsViewModel
    {
        public Item Item { get; set; } = null!;

        public IReadOnlyList<Item> PotentialMatches { get; set; } = Array.Empty<Item>();
        
    }
}
