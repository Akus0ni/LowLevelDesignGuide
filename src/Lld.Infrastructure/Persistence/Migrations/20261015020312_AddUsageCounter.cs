using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lld.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageCounters",
                columns: table => new
                {
                    WindowStart = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Requests = table.Column<long>(type: "INTEGER", nullable: false),
                    Bytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageCounters", x => x.WindowStart);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageCounters");
        }
    }
}
