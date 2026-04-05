using ProcureFlow.Infrastructure.Data;
using ProcureFlow.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Tests;

public class MasterDataSeederTests
{
    [Fact(DisplayName = "MasterData Seeder is idempotent and safe to rerun")]
    public async Task MasterDataSeeder_IsIdempotent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        await MasterDataSeeder.SeedAsync(context);
        await MasterDataSeeder.SeedAsync(context);

        Assert.Equal(4, await context.Categories.CountAsync());
        Assert.Equal(5, await context.AdministrativeUnits.CountAsync());
    }

    [Fact(DisplayName = "MasterData categories preserve parent-child hierarchy without cycles")]
    public async Task MasterData_CategoryHierarchy_IsValid()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        await MasterDataSeeder.SeedAsync(context);

        var categories = await context.Categories.AsNoTracking().ToListAsync();
        foreach (var category in categories)
        {
            Assert.NotEqual(category.Id, category.ParentId ?? -1);
        }

        var ram = categories.Single(x => x.CategoryCode == "IT-RAM");
        Assert.NotNull(ram.ParentId);
    }

    [Fact(DisplayName = "MasterData administrative units support level and parentCode consistency")]
    public async Task MasterData_AdminUnits_LevelAndParentCode_AreConsistent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        await MasterDataSeeder.SeedAsync(context);

        var units = await context.AdministrativeUnits.AsNoTracking().ToListAsync();
        var level4 = units.Single(x => x.Code == "VN-HCM-Q1-BN");
        Assert.Equal(4, level4.Level);
        Assert.Equal("VN-HCM-Q1", level4.ParentCode);
    }
}
