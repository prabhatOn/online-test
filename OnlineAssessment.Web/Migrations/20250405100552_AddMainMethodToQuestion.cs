using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineAssessment.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMainMethodToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainMethod",
                table: "Questions",
                type: "text",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainMethod",
                table: "Questions");
        }
    }
}
