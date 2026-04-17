using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppLocator.Migrations
{
    /// <inheritdoc />
    public partial class TraceDeleteFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteAt",
                table: "traces",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteAt",
                table: "traces");
        }
    }
}
