using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProcureFlow.Infrastructure.Data.Configurations;

public class RfpConfiguration : IEntityTypeConfiguration<Rfp>
{
    public void Configure(EntityTypeBuilder<Rfp> builder)
    {
        builder.ToTable("rfps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.BudgetMin).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BudgetMax).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.PrivacyMode).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items).WithOne(x => x.Rfp).HasForeignKey(x => x.RfpId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Attachments).WithOne(x => x.Rfp).HasForeignKey(x => x.RfpId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RfpItemConfiguration : IEntityTypeConfiguration<RfpItem>
{
    public void Configure(EntityTypeBuilder<RfpItem> builder)
    {
        builder.ToTable("rfp_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasMany(x => x.Specs).WithOne(x => x.RfpItem).HasForeignKey(x => x.RfpItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RfpItemSpecConfiguration : IEntityTypeConfiguration<RfpItemSpec>
{
    public void Configure(EntityTypeBuilder<RfpItemSpec> builder)
    {
        builder.ToTable("rfp_item_specs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ValueText).HasMaxLength(2000);
        builder.Property(x => x.ValueNumber).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Unit).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
    }
}

public class RfpAttachmentConfiguration : IEntityTypeConfiguration<RfpAttachment>
{
    public void Configure(EntityTypeBuilder<RfpAttachment> builder)
    {
        builder.ToTable("rfp_attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
    }
}
