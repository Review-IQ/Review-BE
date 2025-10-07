using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewHub.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAutoRepliedToReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoReplied",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoReplied",
                table: "Reviews");
        }
    }
}
