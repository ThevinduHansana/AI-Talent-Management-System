using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentPlatform.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstReminderSentAt",
                table: "InterviewSchedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecondReminderSentAt",
                table: "InterviewSchedules",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstReminderSentAt",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "SecondReminderSentAt",
                table: "InterviewSchedules");
        }
    }
}
