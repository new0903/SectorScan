using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppLocator.Migrations
{
    /// <inheritdoc />
    public partial class FixTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "traces",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "traces",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
