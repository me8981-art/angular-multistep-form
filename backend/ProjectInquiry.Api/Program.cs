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
            "https://me8981-art.github.io")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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

app.MapGet("/api/submissions", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    return Results.Ok(await db.ProjectSubmissions
        .AsNoTracking()
        .OrderByDescending(item => item.CreatedAtUtc)
        .ToListAsync(cancellationToken));
});

app.MapPost("/api/submissions", async (CreateProjectSubmissionRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var submission = new ProjectSubmission
    {
        Id = Guid.NewGuid(),
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

    db.ProjectSubmissions.Add(submission);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/submissions/{submission.Id}", new
    {
        submission.Id,
        submission.CreatedAtUtc
    });
})
.WithName("CreateProjectSubmission");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProjectSubmissions\" ADD COLUMN IF NOT EXISTS \"Status\" character varying(24) NOT NULL DEFAULT 'New';");
}

app.Run();

static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

public sealed record UpdateSubmissionStatusRequest(string Status);
