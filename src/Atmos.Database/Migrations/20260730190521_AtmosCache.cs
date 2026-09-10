using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atmos.Database.Migrations
{
    /// <inheritdoc />
    public partial class AtmosCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "atmos_cache",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<byte[]>(type: "bytea", nullable: false),
                    expire_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    absolute_expire_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sliding_expiration_ticks = table.Column<long>(type: "bigint", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    create_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atmos_cache", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_atmos_cache_expire_at",
                table: "atmos_cache",
                column: "expire_at");

            migrationBuilder.CreateIndex(
                name: "IX_atmos_cache_tags",
                table: "atmos_cache",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atmos_cache");
        }
    }
}
