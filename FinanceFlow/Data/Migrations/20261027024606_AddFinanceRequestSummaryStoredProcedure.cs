using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceRequestSummaryStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                EXEC(N'
                    CREATE OR ALTER PROCEDURE dbo.usp_FinanceRequest_GetSummaryByRequestor
                        @RequestorId nvarchar(450)
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        SELECT
                            COALESCE(SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END), 0) AS DraftCount,
                            COALESCE(SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END), 0) AS PendingApprovalCount,
                            COALESCE(SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END), 0) AS ApprovedCount,
                            COALESCE(SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END), 0) AS RejectedCount,
                            COALESCE(SUM(Amount), 0) AS TotalAmount,
                            COALESCE(SUM(CASE WHEN Status = 1 THEN Amount ELSE 0 END), 0) AS PendingApprovalAmount,
                            COALESCE(SUM(CASE WHEN Status = 2 THEN Amount ELSE 0 END), 0) AS ApprovedAmount
                        FROM FinanceRequests
                        WHERE RequestorId = @RequestorId;
                    END
                ');
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP PROCEDURE IF EXISTS dbo.usp_FinanceRequest_GetSummaryByRequestor;
                """);
        }
    }
}
