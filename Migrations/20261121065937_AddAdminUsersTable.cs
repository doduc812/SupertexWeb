using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupertexWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanFullAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageProducts = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManagePosts = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageCustomers = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}
