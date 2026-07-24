using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Infrastructure.Data.Configurations;

public class InterviewScheduleConfiguration : IEntityTypeConfiguration<InterviewSchedule>
{
    public void Configure(EntityTypeBuilder<InterviewSchedule> builder)
    {
        builder.ToTable("InterviewSchedules");
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Location).HasMaxLength(256);
        builder.Property(i => i.MeetingLink).HasMaxLength(512);
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.CalendarEventId).HasMaxLength(256);
        builder.Property(i => i.CalendarUid).HasMaxLength(256);
        builder.Property(i => i.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(i => i.ScheduledAt);
        // Supports the recruiter overlap check, which filters on status and orders by start time.
        builder.HasIndex(i => new { i.Status, i.ScheduledAt });

        builder.HasOne(i => i.Application)
            .WithMany(a => a.Interviews)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ScheduledByUser)
            .WithMany()
            .HasForeignKey(i => i.ScheduledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.InterviewerUser)
            .WithMany()
            .HasForeignKey(i => i.InterviewerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InterviewFeedbackConfiguration : IEntityTypeConfiguration<InterviewFeedback>
{
    public void Configure(EntityTypeBuilder<InterviewFeedback> builder)
    {
        builder.ToTable("InterviewFeedbacks");
        builder.Property(f => f.Strengths).HasMaxLength(2000);
        builder.Property(f => f.Weaknesses).HasMaxLength(2000);
        builder.Property(f => f.Comments).HasMaxLength(2000);
        builder.Property(f => f.Recommendation).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(f => f.InterviewSchedule)
            .WithMany(i => i.Feedbacks)
            .HasForeignKey(f => f.InterviewScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.InterviewerUser)
            .WithMany()
            .HasForeignKey(f => f.InterviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CandidateEvaluationConfiguration : IEntityTypeConfiguration<CandidateEvaluation>
{
    public void Configure(EntityTypeBuilder<CandidateEvaluation> builder)
    {
        builder.ToTable("CandidateEvaluations");
        builder.Property(e => e.Comments).HasMaxLength(2000);
        builder.Property(e => e.Decision).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.Application)
            .WithMany(a => a.Evaluations)
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.HiringManager)
            .WithMany(h => h.Evaluations)
            .HasForeignKey(e => e.HiringManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
