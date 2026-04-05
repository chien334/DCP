using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<RfpFinalize> RfpFinalizes => Set<RfpFinalize>();
    public DbSet<RfpFinalizeItem> RfpFinalizeItems => Set<RfpFinalizeItem>();
    public DbSet<RfpContract> RfpContracts => Set<RfpContract>();

    partial void ConfigureFinalizeModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RfpFinalizeConfiguration());
        modelBuilder.ApplyConfiguration(new RfpFinalizeItemConfiguration());
        modelBuilder.ApplyConfiguration(new RfpContractConfiguration());
    }
}
