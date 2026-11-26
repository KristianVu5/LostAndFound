using LostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Data;

public static class DbSeeder
{
    private const string ExampleTitlePrefix = "[Пример]";

    public static async Task SeedAsync(AppDbContext context)
    {
        string[] categoryNames =
        [
            "Портфейли и чанти",
            "Електроника",
            "Ключове",
            "Документи",
            "Дрехи"
        ];

        foreach (var categoryName in categoryNames)
        {
            var categoryExists = await context.Categories
                .AnyAsync(category => category.Name == categoryName);

            if (!categoryExists)
            {
                context.Categories.Add(new Category { Name = categoryName });
            }
        }

        await context.SaveChangesAsync();

        var examplesAlreadyExist = await context.Reports
            .AnyAsync(report => report.Item.Title.StartsWith(ExampleTitlePrefix));

        if (examplesAlreadyExist)
        {
            return;
        }

        var bagsCategoryId = await FindCategoryIdAsync(context, "Портфейли и чанти");
        var electronicsCategoryId = await FindCategoryIdAsync(context, "Електроника");
        var keysCategoryId = await FindCategoryIdAsync(context, "Ключове");
        var documentsCategoryId = await FindCategoryIdAsync(context, "Документи");

        var today = DateOnly.FromDateTime(DateTime.Today);

        var exampleReports = new[]
        {
            new Report
            {
                Item = new Item
                {
                    Title = "[Пример] Черен кожен портфейл",
                    Description = "Черен портфейл с няколко карти вътре.",
                    CategoryId = bagsCategoryId
                },
                Location = "Морска градина, Варна",
                OccurredOn = today.AddDays(-2),
                Type = ItemType.Lost
            },
            new Report
            {
                Item = new Item
                {
                    Title = "[Пример] Намерен черен портфейл",
                    Description = "Намерен е черен кожен портфейл до една пейка.",
                    CategoryId = bagsCategoryId
                },
                Location = "Морска градина, Варна",
                OccurredOn = today.AddDays(-1),
                Type = ItemType.Found
            },
            new Report
            {
                Item = new Item
                {
                    Title = "[Пример] Изгубен смартфон",
                    Description = "Тъмносин смартфон с прозрачен калъф.",
                    CategoryId = electronicsCategoryId
                },
                Location = "Технически университет – Варна",
                OccurredOn = today.AddDays(-3),
                Type = ItemType.Lost
            },
            new Report
            {
                Item = new Item
                {
                    Title = "[Пример] Намерен комплект ключове",
                    Description = "Комплект от три ключа с малък син ключодържател.",
                    CategoryId = keysCategoryId
                },
                Location = "Спирка „Център“, Варна",
                OccurredOn = today.AddDays(-1),
                Type = ItemType.Found
            },
            new Report
            {
                Item = new Item
                {
                    Title = "[Пример] Намерена лична карта",
                    Description = "Лична карта, намерена близо до входа на Морската градина.",
                    CategoryId = documentsCategoryId
                },
                Location = "Входът на Морската градина, Варна",
                OccurredOn = today.AddDays(-4),
                Type = ItemType.Found
            }
        };

        context.Reports.AddRange(exampleReports);
        await context.SaveChangesAsync();
    }

    private static async Task<int> FindCategoryIdAsync(
        AppDbContext context,
        string categoryName)
    {
        return await context.Categories
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .FirstAsync();
    }
}