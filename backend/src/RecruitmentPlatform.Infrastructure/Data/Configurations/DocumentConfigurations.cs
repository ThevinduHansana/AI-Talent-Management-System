using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Infrastructure.Data.Configurations;

public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("Resumes");
        builder.Property(r => r.FileName).HasMaxLength(256).IsRequired();
        builder.Property(r => r.StoredFileName).HasMaxLength(256).IsRequired();
        builder.Property(r => r.FilePath).HasMaxLength(1024).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100);

        builder.HasOne(r => r.Candidate)
            .WithMany(c => c.Resumes)
            .HasForeignKey(r => r.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.IssuingOrganization).HasMaxLength(200);
        builder.Property(c => c.CredentialId).HasMaxLength(200);
        builder.Property(c => c.CredentialUrl).HasMaxLength(512);
        builder.Property(c => c.FileName).HasMaxLength(256);
        builder.Property(c => c.StoredFileName).HasMaxLength(256);
        builder.Property(c => c.FilePath).HasMaxLength(1024);
        builder.Property(c => c.ContentType).HasMaxLength(100);

        builder.HasOne(c => c.Candidate)
            .WithMany(cand => cand.Certificates)
            .HasForeignKey(c => c.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.ToTable("Educations");
        builder.Property(e => e.Institution).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Degree).HasMaxLength(200).IsRequired();
        builder.Property(e => e.FieldOfStudy).HasMaxLength(200);
        builder.Property(e => e.Grade).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasOne(e => e.Candidate)
            .WithMany(c => c.Educations)
            .HasForeignKey(e => e.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("Experiences");
        builder.Property(e => e.Company).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.Candidate)
            .WithMany(c => c.Experiences)
            .HasForeignKey(e => e.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
