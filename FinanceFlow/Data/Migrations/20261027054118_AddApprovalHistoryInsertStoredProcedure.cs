using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalHistoryInsertStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(
                """
                EXEC(N'
                    CREATE OR ALTER PROCEDURE dbo.usp_ApprovalHistory_Insert
                        @FinanceRequestId int,
                        @FromStatus int,
                        @ToStatus int,
                        @ActionByUserId nvarchar(450),
                        @Comment nvarchar(max),
                        @ActionAt datetime2
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        INSERT INTO ApprovalHistories
                        (
                            FinanceRequestId,
                            FromStatus,
                            ToStatus,
                            ActionByUserId,
                            Comment,
                            ActionAt
                        )
                        VALUES
                        (
                            @FinanceRequestId,
                            @FromStatus,
                            @ToStatus,
                            @ActionByUserId,
                            @Comment,
                            @ActionAt
                        );
                    END
                ');
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP PROCEDURE IF EXISTS dbo.usp_ApprovalHistory_Insert;
                """);
        }
    }
}
