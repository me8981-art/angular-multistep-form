using Microsoft.EntityFrameworkCore;
using ProjectInquiry.Api.Models;

namespace ProjectInquiry.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProjectSubmission> ProjectSubmissions => Set<ProjectSubmission>();
    public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var submission = modelBuilder.Entity<ProjectSubmission>();
        submission.HasKey(item => item.Id);
        submission.HasIndex(item => item.TrackingId).IsUnique();
        submission.Property(item => item.TrackingId).HasMaxLength(32).IsRequired();
        submission.Property(item => item.FirstName).HasMaxLength(80).IsRequired();
        submission.Property(item => item.LastName).HasMaxLength(80).IsRequired();
        submission.Property(item => item.Email).HasMaxLength(255).IsRequired();
        submission.Property(item => item.Company).HasMaxLength(160);
        submission.Property(item => item.Role).HasMaxLength(120).IsRequired();
        submission.Property(item => item.ProjectType).HasMaxLength(120).IsRequired();
        submission.Property(item => item.Budget).HasMaxLength(80).IsRequired();
        submission.Property(item => item.Timeline).HasMaxLength(80).IsRequired();
        submission.Property(item => item.Notes).HasMaxLength(4000);
        submission.Property(item => item.Status).HasMaxLength(24).IsRequired();
        submission.Property(item => item.CreatedAtUtc).IsRequired();
        submission.HasIndex(item => item.CreatedAtUtc);
        submission.HasMany(item => item.Files).WithOne(item => item.ProjectSubmission).HasForeignKey(item => item.ProjectSubmissionId).OnDelete(DeleteBehavior.Cascade);

        var file = modelBuilder.Entity<SubmissionFile>();
        file.HasKey(item => item.Id);
        file.Property(item => item.OriginalName).HasMaxLength(255).IsRequired();
        file.Property(item => item.StoredName).HasMaxLength(255).IsRequired();
        file.Property(item => item.ContentType).HasMaxLength(160).IsRequired();
        file.Property(item => item.Kind).HasMaxLength(32).IsRequired();
    }
}
