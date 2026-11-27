using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LostAndFound.Controllers;
using LostAndFound.Data;
using LostAndFound.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;


namespace LostAndFound.Tests;

public class ReportsControllerTests
{
    [Fact]
   public async Task ByPeriod_WhenStartDateIsAfterEndDate_ReturnsValidationErrorAndNoReports()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().Options;
        using var context = new AppDbContext(options);
        var controller = new ReportsController(context);

        var startDate = new DateOnly(2026, 11, 20);
        var endDate = new DateOnly(2026, 11, 10);

        var result = await controller.ByPeriod(startDate, endDate);

        var view = Assert.IsType<ViewResult>(result);
        var reports = Assert.IsAssignableFrom<IEnumerable<Report>>(view.Model);
        Assert.Empty(reports);

        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.TryGetValue("dateTo", out var dateError));
        Assert.Single(dateError!.Errors);
    }
}
