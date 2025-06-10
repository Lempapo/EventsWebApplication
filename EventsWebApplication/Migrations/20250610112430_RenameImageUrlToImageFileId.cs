using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventsWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class RenameImageUrlToImageFileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Events",
                newName: "ImageFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageFileId",
                table: "Events",
                newName: "ImageUrl");
        }
    }
}
