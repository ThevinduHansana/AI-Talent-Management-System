using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Infrastructure.Data.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");
        builder.Property(c => c.Headline).HasMaxLength(200);
        builder.Property(c => c.Summary).HasMaxLength(4000);
        builder.Property(c => c.Location).HasMaxLength(200);
        builder.Property(c => c.LinkedInUrl).HasMaxLength(256);
        builder.Property(c => c.PortfolioUrl).HasMaxLength(256);
        builder.Property(c => c.CurrentPosition).HasMaxLength(200);
        builder.Property(c => c.PreferredCurrency).HasMaxLength(10);
        builder.Property(c => c.ExpectedSalary).HasPrecision(18, 2);
        builder.Property(c => c.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.AvailabilityStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.UserId).IsUnique();

        builder.HasOne(c => c.User)
            .WithOne(u => u.Candidate)
            .HasForeignKey<Candidate>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RecruiterConfiguration : IEntityTypeConfiguration<Recruiter>
{
    public void Configure(EntityTypeBuilder<Recruiter> builder)
    {
        builder.ToTable("Recruiters");
        builder.Property(r => r.JobTitle).HasMaxLength(150);
        builder.Property(r => r.Bio).HasMaxLength(2000);
        builder.HasIndex(r => r.UserId).IsUnique();

        builder.HasOne(r => r.User)
            .WithOne(u => u.Recruiter)
            .HasForeignKey<Recruiter>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Organization)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class HiringManagerConfiguration : IEntityTypeConfiguration<HiringManager>
{
    public void Configure(EntityTypeBuilder<HiringManager> builder)
    {
        builder.ToTable("HiringManagers");
        builder.Property(h => h.JobTitle).HasMaxLength(150);
        builder.HasIndex(h => h.UserId).IsUnique();

        builder.HasOne(h => h.User)
            .WithOne(u => u.HiringManager)
            .HasForeignKey<HiringManager>(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Organization)
            .WithMany()
            .HasForeignKey(h => h.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(h => h.Department)
            .WithMany()
            .HasForeignKey(h => h.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
