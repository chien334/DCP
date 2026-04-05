using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProcureFlow.Infrastructure.Data.Configurations;

public class RfpFinalizeConfiguration : IEntityTypeConfiguration<RfpFinalize>
{
    public void Configure(EntityTypeBuilder<RfpFinalize> builder)
    {
        builder.ToTable("rfp_finalizes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VatRate).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(x => x.SubTotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasOne(x => x.Rfp).WithMany().HasForeignKey(x => x.RfpId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RfpBid).WithMany().HasForeignKey(x => x.RfpBidId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items).WithOne(x => x.RfpFinalize).HasForeignKey(x => x.RfpFinalizeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Contract).WithOne(x => x.RfpFinalize).HasForeignKey<RfpContract>(x => x.RfpFinalizeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RfpId).IsUnique().HasDatabaseName("ux_rfp_finalize_rfp");
    }
}

public class RfpContractConfiguration : IEntityTypeConfiguration<RfpContract>
{
    public void Configure(EntityTypeBuilder<RfpContract> builder)
    {
        builder.ToTable("rfp_contracts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.FileUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasIndex(x => x.RfpFinalizeId).IsUnique().HasDatabaseName("ux_rfp_contract_finalize");
        builder.HasIndex(x => x.ContractNo).IsUnique().HasDatabaseName("ux_rfp_contract_no");
    }
}

public class RfpFinalizeItemConfiguration : IEntityTypeConfiguration<RfpFinalizeItem>
{
    public void Configure(EntityTypeBuilder<RfpFinalizeItem> builder)
    {
        builder.ToTable("rfp_finalize_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasOne(x => x.RfpItem).WithMany().HasForeignKey(x => x.RfpItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RfpBidItem).WithMany().HasForeignKey(x => x.RfpBidItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
