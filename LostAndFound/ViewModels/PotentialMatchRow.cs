namespace LostAndFound.ViewModels
{
    public class PotentialMatchRow
    {
        public int ReportId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public DateOnly OccurredOn { get; init; }
        public int MatchCount { get; init; }
    }
}
