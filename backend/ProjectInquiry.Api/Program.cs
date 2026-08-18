using Microsoft.EntityFrameworkCore;
using ProjectInquiry.Api.Data;
using ProjectInquiry.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(
            "http://localhost:4200",
            "https://me8981-art.github.io",
            "https://4200-i7mf8jm6hvf7j3z9mpngi-7d23be11.us2.manus.computer")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadRoot);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("Frontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/tracking/{trackingId}", async (string trackingId, AppDbContext db, HttpRequest httpRequest, CancellationToken cancellationToken) =>
{
    var submission = await db.ProjectSubmissions.AsNoTracking().Include(item => item.Files).SingleOrDefaultAsync(item => item.TrackingId == trackingId.Trim().ToUpperInvariant(), cancellationToken);
    if (submission is null) return Results.NotFound(new { message = "No submission was found for that tracking ID." });

    return Results.Ok(new
    {
        submission.TrackingId,
        submission.Status,
        submission.CreatedAtUtc,
        Name = $"{submission.FirstName} {submission.LastName}",
        Files = submission.Files.Select(file => new { file.OriginalName, file.Kind, Url = BuildFileUrl(httpRequest, file.StoredName) })
    });
});

app.MapGet("/api/submissions", async (AppDbContext db, HttpRequest httpRequest, CancellationToken cancellationToken) =>
{
    var submissions = await db.ProjectSubmissions
        .AsNoTracking()
        .Include(item => item.Files)
        .OrderByDescending(item => item.CreatedAtUtc)
        .ToListAsync(cancellationToken);

    return Results.Ok(submissions.Select(item => new
    {
        item.Id,
        item.TrackingId,
        item.CreatedAtUtc,
        item.FirstName,
        item.LastName,
        item.Email,
        item.Company,
        item.Role,
        item.ProjectType,
        item.Budget,
        item.Timeline,
        item.Notes,
        item.Status,
        Files = item.Files.Select(file => new { file.Id, file.OriginalName, file.ContentType, file.Size, file.Kind, Url = BuildFileUrl(httpRequest, file.StoredName) })
    }));
});

app.MapPost("/api/submissions", async (HttpRequest httpRequest, AppDbContext db, CancellationToken cancellationToken) =>
{
    var form = await httpRequest.ReadFormAsync(cancellationToken);
    var request = new CreateProjectSubmissionRequest
    {
        FirstName = form["firstName"],
        LastName = form["lastName"],
        Email = form["email"],
        Company = form["company"],
        Role = form["role"],
        ProjectType = form["projectType"],
        Budget = form["budget"],
        Timeline = form["timeline"],
        Notes = form["notes"],
        ProfilePicture = form.Files.GetFile("profilePicture"),
        Attachments = form.Files.GetFiles("attachments")
    };

    var validationErrors = ValidateRequest(request);
    if (validationErrors.Count > 0) return Results.ValidationProblem(validationErrors);

    var submission = new ProjectSubmission
    {
        Id = Guid.NewGuid(),
        TrackingId = await CreateTrackingIdAsync(db, cancellationToken),
        CreatedAtUtc = DateTime.UtcNow,
        FirstName = request.FirstName!.Trim(),
        LastName = request.LastName!.Trim(),
        Email = request.Email!.Trim(),
        Company = CleanOptional(request.Company),
        Role = request.Role!.Trim(),
        ProjectType = request.ProjectType!.Trim(),
        Budget = request.Budget!.Trim(),
        Timeline = request.Timeline!.Trim(),
        Notes = CleanOptional(request.Notes),
        Status = "New"
    };

    var files = new List<IFormFile>();
    if (request.ProfilePicture is not null) files.Add(request.ProfilePicture);
    if (request.Attachments is not null) files.AddRange(request.Attachments);
    if (files.Count > 10) return Results.ValidationProblem(new Dictionary<string, string[]> { ["attachments"] = ["You can upload up to 10 files."] });

    Directory.CreateDirectory(uploadRoot);
    foreach (var file in files)
    {
        var kind = file == request.ProfilePicture ? "Profile picture" : "Attachment";
        var error = ValidateFile(file, kind);
        if (error is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["files"] = [error] });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = File.Create(Path.Combine(uploadRoot, storedName));
        await file.CopyToAsync(stream, cancellationToken);
        submission.Files.Add(new SubmissionFile
        {
            Id = Guid.NewGuid(),
            OriginalName = Path.GetFileName(file.FileName),
            StoredName = storedName,
            ContentType = file.ContentType,
            Size = file.Length,
            Kind = kind,
            UploadedAtUtc = DateTime.UtcNow
        });
    }

    db.ProjectSubmissions.Add(submission);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/submissions/{submission.Id}", new { submission.Id, submission.TrackingId, submission.CreatedAtUtc });
});

app.MapPut("/api/submissions/{id:guid}/status", async (Guid id, UpdateSubmissionStatusRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var allowedStatuses = new[] { "New", "Contacted", "Archived" };
    if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Status must be New, Contacted, or Archived."] });
    }

    var submission = await db.ProjectSubmissions.FindAsync([id], cancellationToken);
    if (submission is null) return Results.NotFound();
    submission.Status = allowedStatuses.First(status => status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(submission);
});

app.MapDelete("/api/submissions/{id:guid}", async (Guid id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var submission = await db.ProjectSubmissions.FindAsync([id], cancellationToken);
    if (submission is null) return Results.NotFound();
    db.ProjectSubmissions.Remove(submission);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProjectSubmissions\" ADD COLUMN IF NOT EXISTS \"TrackingId\" character varying(32);");
    await db.Database.ExecuteSqlRawAsync("UPDATE \"ProjectSubmissions\" SET \"TrackingId\" = 'TMS-2026-' || right(replace(\"Id\"::text, '-', ''), 6) WHERE \"TrackingId\" IS NULL;");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProjectSubmissions\" ALTER COLUMN \"TrackingId\" SET NOT NULL;");
    await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ProjectSubmissions_TrackingId\" ON \"ProjectSubmissions\" (\"TrackingId\");");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProjectSubmissions\" ADD COLUMN IF NOT EXISTS \"Status\" character varying(24) NOT NULL DEFAULT 'New';");
    await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"SubmissionFiles\" (\"Id\" uuid NOT NULL PRIMARY KEY, \"ProjectSubmissionId\" uuid NOT NULL REFERENCES \"ProjectSubmissions\" (\"Id\") ON DELETE CASCADE, \"OriginalName\" character varying(255) NOT NULL, \"StoredName\" character varying(255) NOT NULL, \"ContentType\" character varying(160) NOT NULL, \"Size\" bigint NOT NULL, \"Kind\" character varying(32) NOT NULL, \"UploadedAtUtc\" timestamp with time zone NOT NULL);");
}

app.Run();

static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static Dictionary<string, string[]> ValidateRequest(CreateProjectSubmissionRequest request)
{
    var errors = new Dictionary<string, string[]>();
    foreach (var (name, value) in new[] { ("firstName", request.FirstName), ("lastName", request.LastName), ("email", request.Email), ("role", request.Role), ("projectType", request.ProjectType), ("budget", request.Budget), ("timeline", request.Timeline) })
    {
        if (string.IsNullOrWhiteSpace(value)) errors[name] = ["This field is required."];
    }
    if (!string.IsNullOrWhiteSpace(request.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email)) errors["email"] = ["Enter a valid email address."];
    return errors;
}

static string? ValidateFile(IFormFile file, string kind)
{
    const long maxSize = 10 * 1024 * 1024;
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    var allowed = kind == "Profile picture" ? imageExtensions : imageExtensions.Append(".pdf").ToArray();
    if (file.Length == 0 || file.Length > maxSize) return $"{file.FileName} must be between 1 byte and 10 MB.";
    if (!allowed.Contains(extension)) return $"{file.FileName} is not an allowed file type.";
    return null;
}

static async Task<string> CreateTrackingIdAsync(AppDbContext db, CancellationToken cancellationToken)
{
    var random = Random.Shared.Next(0, 1_000_000);
    var trackingId = $"TMS-{DateTime.UtcNow:yyyy}-{random:000000}";
    while (await db.ProjectSubmissions.AnyAsync(item => item.TrackingId == trackingId, cancellationToken))
    {
        random = Random.Shared.Next(0, 1_000_000);
        trackingId = $"TMS-{DateTime.UtcNow:yyyy}-{random:000000}";
    }
    return trackingId;
}

static string BuildFileUrl(HttpRequest request, string storedName)
{
    var scheme = request.Host.Host.EndsWith("manus.computer", StringComparison.OrdinalIgnoreCase) ? "https" : request.Scheme;
    return $"{scheme}://{request.Host}/uploads/{Uri.EscapeDataString(storedName)}";
}
