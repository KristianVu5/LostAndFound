using System;
using System.Collections.Generic;
using System.Text;
using LostAndFound.Controllers;
using LostAndFound.Data;
using LostAndFound.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LostAndFound.Tests;

public class CategoriesControllerTests
{
    [Fact]
    public async Task Create_WhenModelStateIsInvalid_ReturnsViewWithSubmittedForm()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().Options;
        using var context = new AppDbContext(options);
        var controller = new CategoriesController(context);
        var form = new CategoryForm { Name = "" };

        controller.ModelState.AddModelError(nameof(CategoryForm.Name), "Въведи име.");

        var result = await controller.Create(form);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(form, view.Model);
    }

}
