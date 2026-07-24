using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitmentPlatform.Domain.Entities;

namespace RecruitmentPlatform.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Industry).HasMaxLength(100);
        builder.Property(o => o.Website).HasMaxLength(256);
        builder.Property(o => o.Location).HasMaxLength(200);
        builder.HasIndex(o => o.Name);

        builder.HasMany(o => o.Departments)
            .WithOne(d => d.Organization)
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(d => new { d.OrganizationId, d.Name });
    }
}
