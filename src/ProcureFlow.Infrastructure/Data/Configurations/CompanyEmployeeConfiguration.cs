using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProcureFlow.Infrastructure.Data.Configurations;

public class CompanyEmployeeConfiguration : IEntityTypeConfiguration<CompanyEmployee>
{
    public void Configure(EntityTypeBuilder<CompanyEmployee> builder)
    {
        builder.ToTable("company_employees", table =>
        {
            table.HasCheckConstraint("ck_company_employees_status", "`status` in (1,2,3)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.EmployeeCode).HasColumnName("employee_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(128).IsRequired();

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeCode })
            .IsUnique()
            .HasDatabaseName("ux_company_employees_company_id_employee_code");
    }
}