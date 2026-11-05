using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceRequestReferenceNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "FinanceRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE FinanceRequests
                SET ReferenceNo =
                    'FR-LEGACY' +
                    RIGHT('000000' + CAST(Id AS VARCHAR(6)), 6)
                WHERE ReferenceNo IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNo",
                table: "FinanceRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRequests_ReferenceNo",
                table: "FinanceRequests",
                column: "ReferenceNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinanceRequests_ReferenceNo",
                table: "FinanceRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "FinanceRequests");
        }
    }
}
