using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewCalendarIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalendarSequence",
                table: "InterviewSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CalendarUid",
                table: "InterviewSchedules",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_Status_ScheduledAt",
                table: "InterviewSchedules",
                columns: new[] { "Status", "ScheduledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterviewSchedules_Status_ScheduledAt",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "CalendarSequence",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "CalendarUid",
                table: "InterviewSchedules");
        }
    }
}
