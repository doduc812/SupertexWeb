using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupertexWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddAboutPageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AboutPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: true),
                    MainContent = table.Column<string>(type: "TEXT", nullable: true),
                    MainImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Standard1Title = table.Column<string>(type: "TEXT", nullable: true),
                    Standard1Desc = table.Column<string>(type: "TEXT", nullable: true),
                    Standard2Title = table.Column<string>(type: "TEXT", nullable: true),
                    Standard2Desc = table.Column<string>(type: "TEXT", nullable: true),
                    Standard3Title = table.Column<string>(type: "TEXT", nullable: true),
                    Standard3Desc = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutPages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutPages");
        }
    }
}
