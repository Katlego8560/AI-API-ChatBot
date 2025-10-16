using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_API_ChatBot.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "FirstName", "LastName", "EmailAddress", "CreatedAt" },
                values: new object[]
                {
                "Jag",
                "Admin",
                Constants.ADMIN_USER_EMAIL_ADDRESS,
                DateTime.Now
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "EmailAddress",
                keyValue: Constants.ADMIN_USER_EMAIL_ADDRESS);
        }
    }
}
