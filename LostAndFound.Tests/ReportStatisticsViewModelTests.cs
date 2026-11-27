using System;
using System.Collections.Generic;
using System.Text;
using LostAndFound.ViewModels;
using Xunit;

namespace LostAndFound.Tests;

public class ReportStatisticsViewModelTests
{

    [Theory]
    [InlineData(3, 2, 5)]
    [InlineData(0, 4, 4)]
    [InlineData(0, 0, 0)]
    public void TotalCount_ReturnsSumOfLostAndFound(
        int lostCount, int foundCount, int expectedTotal)
    {

        var statistics = new ReportStatisticsViewModel
        {
            LostCount = lostCount,
            FoundCount = foundCount
        };

        var actualTotal = statistics.TotalCount;

        Assert.Equal(expectedTotal, actualTotal);
    }
}
