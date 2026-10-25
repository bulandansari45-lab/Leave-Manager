using System;
using LeaveManagement.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Data.Migrations.Leave
{
    [DbContext(typeof(LeaveDbContext))]
    [Migration("20261025093000_AddLeaveReviewDetails")]
    public partial class AddLeaveReviewDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "RejectionReason", table: "LeaveRequests", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "MoreInfoRequest", table: "LeaveRequests", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(name: "EmployeeResponse", table: "LeaveRequests", type: "nvarchar(500)", maxLength: 500, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RejectionReason", table: "LeaveRequests");
            migrationBuilder.DropColumn(name: "MoreInfoRequest", table: "LeaveRequests");
            migrationBuilder.DropColumn(name: "EmployeeResponse", table: "LeaveRequests");
        }
    }
}