using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewHub.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAISettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AISettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EnableAutoReply = table.Column<bool>(type: "bit", nullable: false),
                    AutoReplyToPositiveReviews = table.Column<bool>(type: "bit", nullable: false),
                    AutoReplyToNeutralReviews = table.Column<bool>(type: "bit", nullable: false),
                    AutoReplyToNegativeReviews = table.Column<bool>(type: "bit", nullable: false),
                    AutoReplyToQuestions = table.Column<bool>(type: "bit", nullable: false),
                    EnableAISuggestions = table.Column<bool>(type: "bit", nullable: false),
                    EnableSentimentAnalysis = table.Column<bool>(type: "bit", nullable: false),
                    EnableCompetitorAnalysis = table.Column<bool>(type: "bit", nullable: false),
                    EnableInsightsGeneration = table.Column<bool>(type: "bit", nullable: false),
                    ResponseTone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseLength = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AISettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AISettings_UserId",
                table: "AISettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AISettings");
        }
    }
}
