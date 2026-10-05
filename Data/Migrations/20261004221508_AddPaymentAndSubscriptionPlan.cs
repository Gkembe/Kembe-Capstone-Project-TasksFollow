using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasksFollow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_SubscriptionPlan_SubscriptionPlanId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionPlan",
                table: "SubscriptionPlan");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlan",
                newName: "SubscriptionPlans");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionPlans",
                table: "SubscriptionPlans",
                column: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_SubscriptionPlans_SubscriptionPlanId",
                table: "Payments",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "SubscriptionPlanId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_SubscriptionPlans_SubscriptionPlanId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionPlans",
                table: "SubscriptionPlans");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlans",
                newName: "SubscriptionPlan");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionPlan",
                table: "SubscriptionPlan",
                column: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_SubscriptionPlan_SubscriptionPlanId",
                table: "Payments",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlan",
                principalColumn: "SubscriptionPlanId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
