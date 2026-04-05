using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureFlow.Infrastructure.Data.Migrations;

public partial class AddCompanyEmployeeFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "companies",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                LegalName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                ShortName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                TaxCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                Phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                created_by = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                updated_by = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_companies", x => x.Id);
                table.CheckConstraint("ck_companies_status", "`status` in (0,1,2,3)");
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "company_addresses",
            columns: table => new
            {
                CompanyId = table.Column<int>(type: "int", nullable: false),
                Country = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                Province = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                District = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                Ward = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                AddressLine = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                PostalCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_company_addresses", x => x.CompanyId);
                table.ForeignKey(
                    name: "FK_company_addresses_companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "company_employees",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                company_id = table.Column<int>(type: "int", nullable: false),
                employee_code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                full_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                created_by = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                updated_by = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_company_employees", x => x.Id);
                table.CheckConstraint("ck_company_employees_status", "`status` in (1,2,3)");
                table.ForeignKey(
                    name: "FK_company_employees_companies_company_id",
                    column: x => x.company_id,
                    principalTable: "companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "ux_companies_tax_code",
            table: "companies",
            column: "TaxCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_company_employees_company_id_employee_code",
            table: "company_employees",
            columns: new[] { "company_id", "employee_code" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "company_addresses");
        migrationBuilder.DropTable(name: "company_employees");
        migrationBuilder.DropTable(name: "companies");
    }
}