using System.ComponentModel.DataAnnotations;
using LostAndFound.Data;

namespace LostAndFound.Models
{
    public class ItemClaim
    {
        public int Id { get; set; }

        public int ReportId { get; set; }

        public string ClaimantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Съобщението е задължително.")]
        [StringLength(1000, ErrorMessage = "Съобщението не може да е по-дълго от 1000 символа.")]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public ItemClaimStatus Status { get; set; } = ItemClaimStatus.Pending;

        public Report Report { get; set; } = null!;

        public ApplicationUser Claimant { get; set; } = null!;
    }
}
