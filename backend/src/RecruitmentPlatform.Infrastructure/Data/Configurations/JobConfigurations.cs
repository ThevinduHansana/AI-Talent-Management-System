using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Infrastructure.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");
        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Description).IsRequired();
        builder.Property(j => j.Location).HasMaxLength(200);
        builder.Property(j => j.Currency).HasMaxLength(10);
        builder.Property(j => j.SalaryMin).HasPrecision(18, 2);
        builder.Property(j => j.SalaryMax).HasPrecision(18, 2);
        builder.Property(j => j.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.ExperienceLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.Title);

        builder.HasOne(j => j.Organization)
            .WithMany(o => o.Jobs)
            .HasForeignKey(j => j.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Department)
            .WithMany(d => d.Jobs)
            .HasForeignKey(j => j.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.Recruiter)
            .WithMany(r => r.Jobs)
            .HasForeignKey(j => j.RecruiterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("Applications");
        builder.Property(a => a.CoverLetter).HasMaxLength(4000);
        builder.Property(a => a.RecruiterNotes).HasMaxLength(4000);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        // A candidate can apply to a given job only once.
        builder.HasIndex(a => new { a.JobId, a.CandidateId }).IsUnique();
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Candidate)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Resume)
            .WithMany()
            .HasForeignKey(a => a.ResumeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SavedJobConfiguration : IEntityTypeConfiguration<SavedJob>
{
    public void Configure(EntityTypeBuilder<SavedJob> builder)
    {
        builder.ToTable("SavedJobs");
        builder.HasIndex(s => new { s.CandidateId, s.JobId }).IsUnique();

        builder.HasOne(s => s.Candidate)
            .WithMany(c => c.SavedJobs)
            .HasForeignKey(s => s.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Job)
            .WithMany(j => j.SavedByCandidates)
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
