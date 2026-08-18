using System.ComponentModel.DataAnnotations;

namespace ProjectInquiry.Api.Models;

public sealed class ProjectSubmission
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string Budget { get; set; } = string.Empty;
    public string Timeline { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class CreateProjectSubmissionRequest
{
    [Required, MaxLength(80)] public string? FirstName { get; init; }
    [Required, MaxLength(80)] public string? LastName { get; init; }
    [Required, EmailAddress, MaxLength(255)] public string? Email { get; init; }
    [MaxLength(160)] public string? Company { get; init; }
    [Required, MaxLength(120)] public string? Role { get; init; }
    [Required, MaxLength(120)] public string? ProjectType { get; init; }
    [Required, MaxLength(80)] public string? Budget { get; init; }
    [Required, MaxLength(80)] public string? Timeline { get; init; }
    [MaxLength(4000)] public string? Notes { get; init; }
}
