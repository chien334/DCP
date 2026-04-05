using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProcureFlow.Infrastructure.Data.Configurations;

public class RfpVendorParticipationConfiguration : IEntityTypeConfiguration<RfpVendorParticipation>
{
    public void Configure(EntityTypeBuilder<RfpVendorParticipation> builder)
    {
        builder.ToTable("rfp_vendor_participations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.InviteAt).IsRequired();

        builder.HasOne(x => x.Rfp).WithMany().HasForeignKey(x => x.RfpId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RfpId, x.CompanyId }).IsUnique().HasDatabaseName("ux_rfp_vendor_rfp_company");
    }
}

public class RfpBidConfiguration : IEntityTypeConfiguration<RfpBid>
{
    public void Configure(EntityTypeBuilder<RfpBid> builder)
    {
        builder.ToTable("rfp_bids");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VatRate).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(x => x.SubTotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Proposal).HasMaxLength(4000);
        builder.Property(x => x.PrivacyMode).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasOne(x => x.Rfp).WithMany().HasForeignKey(x => x.RfpId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items).WithOne(x => x.RfpBid).HasForeignKey(x => x.RfpBidId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RfpId, x.CompanyId }).IsUnique().HasDatabaseName("ux_rfp_bids_rfp_company");
    }
}

public class RfpBidItemConfiguration : IEntityTypeConfiguration<RfpBidItem>
{
    public void Configure(EntityTypeBuilder<RfpBidItem> builder)
    {
        builder.ToTable("rfp_bid_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Brand).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasOne(x => x.RfpItem).WithMany().HasForeignKey(x => x.RfpItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Specs).WithOne(x => x.RfpBidItem).HasForeignKey(x => x.RfpBidItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RfpBidItemSpecConfiguration : IEntityTypeConfiguration<RfpBidItemSpec>
{
    public void Configure(EntityTypeBuilder<RfpBidItemSpec> builder)
    {
        builder.ToTable("rfp_bid_item_specs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ValueText).HasMaxLength(2000);
        builder.Property(x => x.ValueNumber).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Unit).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
    }
}
