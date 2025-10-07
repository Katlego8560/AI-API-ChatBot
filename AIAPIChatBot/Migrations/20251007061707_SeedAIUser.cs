using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_API_ChatBot.Migrations
{
    /// <inheritdoc />
    public partial class SeedAIUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "FirstName", "LastName", "EmailAddress", "CreatedAt" },
                values: new object[]
                {
                "Jag",
                "AI",
                "aichatbot@jagmethod.com",
                DateTime.Now
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "EmailAddress",
                keyValue: "aichatbot@jagmethod.com");
        }
    }
}