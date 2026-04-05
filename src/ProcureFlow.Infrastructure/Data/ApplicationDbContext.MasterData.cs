using ProcureFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProcureFlow.Infrastructure.Data;

public partial class ApplicationDbContext
{
    partial void ConfigureMasterDataModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CategoryCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CategoryName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1024);
            entity.Property(x => x.TreePath).HasMaxLength(512).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.DisplayOrder).IsRequired();
            entity.Property(x => x.CreateAt).IsRequired();

            entity.HasIndex(x => x.CategoryCode).IsUnique();
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdministrativeUnit>(entity =>
        {
            entity.ToTable("administrative_units");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ParentCode).HasMaxLength(64);
            entity.Property(x => x.Level).IsRequired();
            entity.Property(x => x.CreateAt).IsRequired();

            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.Level, x.ParentCode });
        });
    }
}
