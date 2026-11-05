using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceRequestRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FinanceRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FinanceRequests");
        }
    }
}
