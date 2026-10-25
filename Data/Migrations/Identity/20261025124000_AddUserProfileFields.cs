using LeaveManagement.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Data.Migrations.Identity
{
    [DbContext(typeof(AuthDbContext))]
    [Migration("20261025124000_AddUserProfileFields")]
    public partial class AddUserProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Designation", table: "AspNetUsers", type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ProfilePhotoPath", table: "AspNetUsers", type: "nvarchar(300)", maxLength: 300, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Designation", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "ProfilePhotoPath", table: "AspNetUsers");
        }
    }
}