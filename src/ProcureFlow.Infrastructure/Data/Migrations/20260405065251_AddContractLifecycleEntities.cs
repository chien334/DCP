using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContractLifecycleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rfp_contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RfpFinalizeId = table.Column<int>(type: "int", nullable: false),
                    ContractNo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileUrl = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyerSign = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BuyerSignAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VendorSign = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VendorSignAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Note = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfp_contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfp_contracts_rfp_finalizes_RfpFinalizeId",
                        column: x => x.RfpFinalizeId,
                        principalTable: "rfp_finalizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ux_rfp_contract_finalize",
                table: "rfp_contracts",
                column: "RfpFinalizeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rfp_contract_no",
                table: "rfp_contracts",
                column: "ContractNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfp_contracts");
        }
    }
}
