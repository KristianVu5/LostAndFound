using LostAndFound.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing connection string: DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
     options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames = ["User", "Admin"];

    foreach (var roleName in roleNames)
    {
        if(await roleManager.RoleExistsAsync(roleName))
        {
            continue;
        }

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Ролята '{roleName}' не може дда бъде създадена: {errors}");
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var adminEmail = app.Configuration["AdminEmail"];

    if (string.IsNullOrWhiteSpace(adminEmail))
    {
        throw new InvalidOperationException(
            "AdminEmail не е зададен в appsettings.Development.json.");
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser is null)
    {
        throw new InvalidOperationException(
            $"Не е намерен регистриран акаунт с имейл {adminEmail}.");
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        var result = await userManager.AddToRoleAsync(adminUser, "Admin");

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Неуспешно назначаване на администраторската роля: {errors}");
        }
    }
}
app.Run();
