using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data;

public partial class ApplicationDbContext
{
    public DbSet<Rfp> Rfps => Set<Rfp>();
    public DbSet<RfpItem> RfpItems => Set<RfpItem>();
    public DbSet<RfpItemSpec> RfpItemSpecs => Set<RfpItemSpec>();
    public DbSet<RfpAttachment> RfpAttachments => Set<RfpAttachment>();

    partial void ConfigureRfpModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RfpConfiguration());
        modelBuilder.ApplyConfiguration(new RfpItemConfiguration());
        modelBuilder.ApplyConfiguration(new RfpItemSpecConfiguration());
        modelBuilder.ApplyConfiguration(new RfpAttachmentConfiguration());
    }
}
