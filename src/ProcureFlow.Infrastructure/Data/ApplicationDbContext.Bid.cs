using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<RfpVendorParticipation> RfpVendorParticipations => Set<RfpVendorParticipation>();
    public DbSet<RfpBid> RfpBids => Set<RfpBid>();
    public DbSet<RfpBidItem> RfpBidItems => Set<RfpBidItem>();
    public DbSet<RfpBidItemSpec> RfpBidItemSpecs => Set<RfpBidItemSpec>();

    partial void ConfigureBidModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RfpVendorParticipationConfiguration());
        modelBuilder.ApplyConfiguration(new RfpBidConfiguration());
        modelBuilder.ApplyConfiguration(new RfpBidItemConfiguration());
        modelBuilder.ApplyConfiguration(new RfpBidItemSpecConfiguration());
    }
}
