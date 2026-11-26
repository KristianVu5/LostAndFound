namespace LostAndFound.ViewModels
{
    public class ReportStatisticsViewModel
    {
        public int LostCount { get; init; }
        public int FoundCount { get; init; }

        public int TotalCount => LostCount + FoundCount;
    }
}
