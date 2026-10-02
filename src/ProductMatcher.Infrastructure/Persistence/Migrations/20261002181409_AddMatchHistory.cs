using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductMatcher.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Query = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NormalizedQuery = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_history_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_history_CustomerId_ConfirmedAt",
                table: "match_history",
                columns: new[] { "CustomerId", "ConfirmedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_match_history_CustomerId_NormalizedQuery",
                table: "match_history",
                columns: new[] { "CustomerId", "NormalizedQuery" });

            migrationBuilder.CreateIndex(
                name: "IX_match_history_CustomerId_ProductId",
                table: "match_history",
                columns: new[] { "CustomerId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_match_history_ProductId",
                table: "match_history",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_history");
        }
    }
}
