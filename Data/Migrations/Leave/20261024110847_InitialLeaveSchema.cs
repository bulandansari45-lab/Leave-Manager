using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Data.Migrations.Leave
{
    /// <inheritdoc />
    public partial class InitialLeaveSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing installations were created with EnsureCreated. If this table is
            // already present, let EF record this migration without recreating it.
            // New databases still receive the complete initial schema.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[LeaveRequests]
                    (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [EmployeeName] nvarchar(100) NOT NULL,
                        [Email] nvarchar(160) NOT NULL,
                        [LeaveType] nvarchar(40) NOT NULL,
                        [StartDate] datetime2 NOT NULL,
                        [EndDate] datetime2 NOT NULL,
                        [Reason] nvarchar(500) NOT NULL,
                        [Status] nvarchar(20) NOT NULL,
                        [SubmittedAt] datetime2 NOT NULL,
                        [DecisionByName] nvarchar(100) NULL,
                        [DecisionByEmail] nvarchar(160) NULL,
                        [DecisionAt] datetime2 NULL,
                        CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id])
                    );
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRequests");
        }
    }
}
