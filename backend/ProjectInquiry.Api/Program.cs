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
        Notes = CleanOptional(request.Notes)
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
}

app.Run();

static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
