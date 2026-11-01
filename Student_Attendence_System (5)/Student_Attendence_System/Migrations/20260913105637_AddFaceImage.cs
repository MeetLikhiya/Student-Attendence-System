using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Attendence_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaceImagePath",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceImagePath",
                table: "Students");
        }
    }
}
