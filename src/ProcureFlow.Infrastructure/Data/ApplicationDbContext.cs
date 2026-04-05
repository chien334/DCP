using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyAddress> CompanyAddresses => Set<CompanyAddress>();
    public DbSet<CompanyEmployee> CompanyEmployees => Set<CompanyEmployee>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AdministrativeUnit> AdministrativeUnits => Set<AdministrativeUnit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyEmployeeConfiguration());
        ConfigureCompanyEmployeeModel(modelBuilder);
        ConfigureMasterDataModel(modelBuilder);
        ConfigureRfpModel(modelBuilder);
        ConfigureBidModel(modelBuilder);
        ConfigureFinalizeModel(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    partial void ConfigureCompanyEmployeeModel(ModelBuilder modelBuilder);
    partial void ConfigureMasterDataModel(ModelBuilder modelBuilder);
    partial void ConfigureRfpModel(ModelBuilder modelBuilder);
    partial void ConfigureBidModel(ModelBuilder modelBuilder);
    partial void ConfigureFinalizeModel(ModelBuilder modelBuilder);
}