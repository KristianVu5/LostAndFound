using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using LostAndFound.Controllers;
using LostAndFound.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LostAndFound.Tests;

public class AdminsControllerTests
{
    [Fact]
    public async Task Index_WhenAdminEmailDoesNotMatch_ReturnsForbid()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminEmail"] = "main@example.com"
        })
        .Build();

        var user = new ApplicationUser { Email = "other@example.com" };

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") },
                                            authenticationType: "Test");
        var principal = new ClaimsPrincipal(identity);
        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);
        
        userManager.Setup(manager => manager.GetUserAsync(principal)).ReturnsAsync(user);

        var controller = new AdminsController(userManager.Object, configuration);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var result = await controller.Index();

        Assert.IsType<ForbidResult>(result);
    }
}
