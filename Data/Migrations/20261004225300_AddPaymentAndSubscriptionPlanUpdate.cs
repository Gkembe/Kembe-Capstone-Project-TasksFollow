using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasksFollow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndSubscriptionPlanUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "SubscriptionPlans");
        }
    }
}
