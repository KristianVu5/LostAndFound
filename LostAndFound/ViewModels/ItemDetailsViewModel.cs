using LostAndFound.Models;

namespace LostAndFound.ViewModels
{
    public class ItemDetailsViewModel
    {
        public Report Report { get; set; } = null!;
        public bool CanManage { get; set; }

        public IReadOnlyList<Report> PotentialMatches { get; set; } = Array.Empty<Report>();
        
    }
}
