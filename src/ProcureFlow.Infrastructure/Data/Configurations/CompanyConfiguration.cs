using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProcureFlow.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies", table =>
        {
            table.HasCheckConstraint("ck_companies_status", "`status` in (0,1,2,3)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.LegalName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ShortName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TaxCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.TaxCode).IsUnique().HasDatabaseName("ux_companies_tax_code");

        builder.HasOne(x => x.Address)
            .WithOne(x => x.Company)
            .HasForeignKey<CompanyAddress>(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}