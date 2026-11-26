using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupertexWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroSectionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeroSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BadgeText = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Stat1Value = table.Column<string>(type: "TEXT", nullable: true),
                    Stat1Label = table.Column<string>(type: "TEXT", nullable: true),
                    Stat2Value = table.Column<string>(type: "TEXT", nullable: true),
                    Stat2Label = table.Column<string>(type: "TEXT", nullable: true),
                    Stat3Value = table.Column<string>(type: "TEXT", nullable: true),
                    Stat3Label = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroSections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeroSections");
        }
    }
}
