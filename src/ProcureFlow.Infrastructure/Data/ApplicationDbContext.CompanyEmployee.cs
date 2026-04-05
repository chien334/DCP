using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data;

public partial class ApplicationDbContext
{
    partial void ConfigureCompanyEmployeeModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcureFlow.Core.Entities.CompanyAddress>(entity =>
        {
            entity.ToTable("company_addresses");
            entity.HasKey(x => x.CompanyId);
            entity.Property(x => x.Country).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Province).HasMaxLength(128).IsRequired();
            entity.Property(x => x.District).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Ward).HasMaxLength(128).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
        });
    }
}