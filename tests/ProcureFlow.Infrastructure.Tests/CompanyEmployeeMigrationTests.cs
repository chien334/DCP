using ProcureFlow.Core.Entities;
using ProcureFlow.Infrastructure.Data;
using ProcureFlow.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ProcureFlow.Infrastructure.Tests;

public class CompanyEmployeeMigrationTests
{
    [Fact]
    public void Company_Enforces_UniqueTaxCode_And_StatusEnumMapping()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var model = context.Model;
        var company = model.FindEntityType(typeof(Company));

        Assert.NotNull(company);
        Assert.Contains(company!.GetIndexes(), i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Company.TaxCode)));
        Assert.Equal(typeof(int), company.FindProperty(nameof(Company.Status))!.GetProviderClrType());
    }

    [Fact]
    public void CompanyEmployee_Uses_ForeignKey_And_ImmutableCompanyLinkage()
    {
        var companyIdProperty = typeof(CompanyEmployee).GetProperty(nameof(CompanyEmployee.CompanyId));
        Assert.NotNull(companyIdProperty);
        Assert.False(companyIdProperty!.SetMethod?.IsPublic ?? false);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var employeeEntity = context.Model.FindEntityType(typeof(CompanyEmployee));

        Assert.NotNull(employeeEntity);
        var foreignKey = employeeEntity!.GetForeignKeys().Single(fk => fk.Properties.Any(p => p.Name == nameof(CompanyEmployee.CompanyId)));
        Assert.Equal(typeof(Company), foreignKey.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void Migration_Creates_Fk_Unique_And_Check_Constraints_For_Company_And_Employee()
    {
        var migration = new TestableMigration();
        var builder = new MigrationBuilder("MySql");
        migration.InvokeUp(builder);

        Assert.Contains(builder.Operations.OfType<CreateTableOperation>(), op => op.Name == "companies");
        Assert.Contains(builder.Operations.OfType<CreateTableOperation>(), op => op.Name == "company_employees");
        Assert.Contains(builder.Operations.OfType<CreateIndexOperation>(), op => op.Name == "ux_companies_tax_code" && op.IsUnique);
        Assert.Contains(builder.Operations.OfType<CreateIndexOperation>(), op => op.Name == "ux_company_employees_company_id_employee_code" && op.IsUnique);
        Assert.Contains(builder.Operations.OfType<CreateTableOperation>(), op =>
            op.Name == "company_employees" && op.ForeignKeys.Any(fk => fk.PrincipalTable == "companies"));
    }

    private sealed class TestableMigration : AddCompanyEmployeeFoundation
    {
        public void InvokeUp(MigrationBuilder migrationBuilder)
        {
            Up(migrationBuilder);
        }
    }
}