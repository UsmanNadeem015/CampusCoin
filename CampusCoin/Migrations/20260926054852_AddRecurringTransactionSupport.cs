using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCoin.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransactionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextRecurringDate",
                table: "Transactions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextRecurringDate",
                table: "Transactions");
        }
    }
}
