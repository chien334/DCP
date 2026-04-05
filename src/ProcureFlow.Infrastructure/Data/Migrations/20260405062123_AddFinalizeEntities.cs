using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalizeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rfp_finalizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RfpId = table.Column<int>(type: "int", nullable: false),
                    RfpBidId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_rfp_finalizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfp_finalizes_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfp_finalizes_rfp_bids_RfpBidId",
                        column: x => x.RfpBidId,
                        principalTable: "rfp_bids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfp_finalizes_rfps_RfpId",
                        column: x => x.RfpId,
                        principalTable: "rfps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rfp_finalize_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RfpFinalizeId = table.Column<int>(type: "int", nullable: false),
                    RfpItemId = table.Column<int>(type: "int", nullable: false),
                    RfpBidItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
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
                    table.PrimaryKey("PK_rfp_finalize_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfp_finalize_items_rfp_bid_items_RfpBidItemId",
                        column: x => x.RfpBidItemId,
                        principalTable: "rfp_bid_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfp_finalize_items_rfp_finalizes_RfpFinalizeId",
                        column: x => x.RfpFinalizeId,
                        principalTable: "rfp_finalizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rfp_finalize_items_rfp_items_RfpItemId",
                        column: x => x.RfpItemId,
                        principalTable: "rfp_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_rfp_finalize_items_RfpBidItemId",
                table: "rfp_finalize_items",
                column: "RfpBidItemId");

            migrationBuilder.CreateIndex(
                name: "IX_rfp_finalize_items_RfpFinalizeId",
                table: "rfp_finalize_items",
                column: "RfpFinalizeId");

            migrationBuilder.CreateIndex(
                name: "IX_rfp_finalize_items_RfpItemId",
                table: "rfp_finalize_items",
                column: "RfpItemId");

            migrationBuilder.CreateIndex(
                name: "IX_rfp_finalizes_CompanyId",
                table: "rfp_finalizes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_rfp_finalizes_RfpBidId",
                table: "rfp_finalizes",
                column: "RfpBidId");

            migrationBuilder.CreateIndex(
                name: "ux_rfp_finalize_rfp",
                table: "rfp_finalizes",
                column: "RfpId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfp_finalize_items");

            migrationBuilder.DropTable(
                name: "rfp_finalizes");
        }
    }
}
