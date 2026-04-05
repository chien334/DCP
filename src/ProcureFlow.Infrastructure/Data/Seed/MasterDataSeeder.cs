using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data.Seed;

public static class MasterDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var categories = new List<Category>
        {
            new() { Id = 1, CategoryCode = "IT", CategoryName = "IT Equipment", ParentId = null, TreePath = "/IT", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, CategoryCode = "IT-RAM", CategoryName = "RAM", ParentId = 1, TreePath = "/IT/IT-RAM", DisplayOrder = 1, IsActive = true },
            new() { Id = 3, CategoryCode = "IT-SSD", CategoryName = "SSD", ParentId = 1, TreePath = "/IT/IT-SSD", DisplayOrder = 2, IsActive = true },
            new() { Id = 4, CategoryCode = "OFFICE", CategoryName = "Office Supplies", ParentId = null, TreePath = "/OFFICE", DisplayOrder = 2, IsActive = true }
        };

        // Guard against invalid parent references and direct self-cycle in seed set.
        var byId = categories.ToDictionary(x => x.Id);
        foreach (var category in categories)
        {
            if (category.ParentId is null)
            {
                continue;
            }

            if (category.ParentId == category.Id || !byId.ContainsKey(category.ParentId.Value))
            {
                throw new InvalidOperationException($"Invalid category parent reference for id {category.Id}");
            }
        }

        foreach (var category in categories)
        {
            var existing = await dbContext.Categories.FirstOrDefaultAsync(x => x.CategoryCode == category.CategoryCode, cancellationToken);
            if (existing is null)
            {
                dbContext.Categories.Add(category);
                continue;
            }

            existing.CategoryName = category.CategoryName;
            existing.Description = category.Description;
            existing.IsActive = category.IsActive;
            existing.ParentId = category.ParentId;
            existing.TreePath = category.TreePath;
            existing.DisplayOrder = category.DisplayOrder;
        }

        var units = new List<AdministrativeUnit>
        {
            new() { Id = 1, Code = "VN", Name = "Viet Nam", ParentCode = null, Level = 1 },
            new() { Id = 2, Code = "VN-HCM", Name = "Ho Chi Minh", ParentCode = "VN", Level = 2 },
            new() { Id = 3, Code = "VN-HCM-Q1", Name = "District 1", ParentCode = "VN-HCM", Level = 3 },
            new() { Id = 4, Code = "VN-HCM-Q1-BN", Name = "Ben Nghe", ParentCode = "VN-HCM-Q1", Level = 4 },
            new() { Id = 5, Code = "VN-HN", Name = "Ha Noi", ParentCode = "VN", Level = 2 }
        };

        var unitCodeSet = units.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in units)
        {
            if (string.IsNullOrWhiteSpace(unit.ParentCode))
            {
                continue;
            }

            if (!unitCodeSet.Contains(unit.ParentCode))
            {
                throw new InvalidOperationException($"Invalid administrative unit parent code for {unit.Code}");
            }
        }

        foreach (var unit in units)
        {
            var existing = await dbContext.AdministrativeUnits.FirstOrDefaultAsync(x => x.Code == unit.Code, cancellationToken);
            if (existing is null)
            {
                dbContext.AdministrativeUnits.Add(unit);
                continue;
            }

            existing.Name = unit.Name;
            existing.ParentCode = unit.ParentCode;
            existing.Level = unit.Level;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
