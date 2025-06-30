using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IllustratedBook.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSlugColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Books");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Sections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
