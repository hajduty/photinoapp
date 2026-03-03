using JobTracker.Application.Features.Classifications;
using JobTracker.Application.Features.Tags;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Infrastructure.Data;

public static class SeedData
{
    private static JinaEmbeddingService _jinaService = null!;

    public static void Initialize(IDbContextFactory<AppDbContext> factory, JinaEmbeddingService jinaEmbeddingService)
    {
        _jinaService = jinaEmbeddingService;
        using var context = factory.CreateDbContext();

        if (!context.Prototypes.Any()) {
            SeedClassificationsAndPrototypes(context);
            context.SaveChanges();
        }

        if (context.JobTrackers.Any())
            return;

        context.JobTrackers.AddRange(
            new Features.JobTracker.JobTracker { Keyword = "utvecklare" },
            new Features.JobTracker.JobTracker { Keyword = "developer"}
        );

        if (context.Notifications.Any())
            return;

        context.Notifications.Add(
            new Features.Notification.Notification { Description = "Welcome", Title = "System notification", Type = Features.Notification.NotificationType.None }
        );

        if (context.Tags.Any())
            return;

        context.Tags.AddRange(
            new Tag { Name = "JavaScript", Color = "#f7df1e" },
            new Tag { Name = "TypeScript", Color = "#3178c6" },
            new Tag { Name = "HTML", Color = "#e34c26" },
            new Tag { Name = "CSS", Color = "#264de4" },
            new Tag { Name = "PHP", Color = "#777bb4" },
            new Tag { Name = "Ruby", Color = "#cc342d" },

            new Tag { Name = "C#", Color = "#512bd4" },
            new Tag { Name = "Java", Color = "#007396" },
            new Tag { Name = "C++", Color = "#00599c" },
            new Tag { Name = "C", Color = "#555555" },
            new Tag { Name = "Python", Color = "#3776ab" },
            new Tag { Name = "Go", Color = "#00add8" },
            new Tag { Name = "Rust", Color = "#dea584" },
            new Tag { Name = "Swift", Color = "#fa7343" },
            new Tag { Name = "Kotlin", Color = "#7f52ff" },

            new Tag { Name = "React", Color = "#61dafb" },
            new Tag { Name = "Next.js", Color = "#000000" },
            new Tag { Name = "Vue", Color = "#42b883" },
            new Tag { Name = "Angular", Color = "#dd0031" },
            new Tag { Name = "Svelte", Color = "#ff3e00" },

            new Tag { Name = ".NET", Color = "#512bd4" },
            new Tag { Name = "ASP.NET Core", Color = "#5c2d91" },
            new Tag { Name = "Spring", Color = "#6db33f" },
            new Tag { Name = "Node.js", Color = "#339933" },
            new Tag { Name = "Django", Color = "#092e20" },
            new Tag { Name = "Flask", Color = "#000000" },
            new Tag { Name = "Laravel", Color = "#ff2d20" },
            new Tag { Name = "Ruby on Rails", Color = "#cc0000" },

            new Tag { Name = "Electron", Color = "#47848f" },
            new Tag { Name = ".NET MAUI", Color = "#512bd4" },
            new Tag { Name = "WPF", Color = "#68217a" },
            new Tag { Name = "WinForms", Color = "#0078d4" }
        );

        context.SaveChanges();
    }

    private static void SeedClassificationsAndPrototypes(AppDbContext context)
    {
        var classificationData = new[]
        {
            new Classification { Id = 1, Name = "Job Title & Intro",       Color = null },
            new Classification { Id = 2, Name = "Company & Culture",       Color = null },
            new Classification { Id = 3, Name = "Must-Haves",              Color = "#3F1D1D" },
            new Classification { Id = 4, Name = "Technologies",            Color = "#1E3A34" },
            new Classification { Id = 5, Name = "Responsibilities",        Color = "#1E293B" },
            new Classification { Id = 6, Name = "Location",                Color = null },
            new Classification { Id = 7, Name = "Application Process",     Color = "#3A2E1F" }
        };

        context.Classifications.AddRange(classificationData);

        var prototypes = new List<Prototype>
        {
            // Job Title & Intro (ClassificationId = 1)
            CreatePrototype(1, 1, "Senior Full-stack Developer - Join our core product team"),
            CreatePrototype(2, 1, "Frontend Developer position focusing on React and modern web technologies"),
            CreatePrototype(3, 1, "Backend Engineer wanted for scalable cloud solutions"),
            CreatePrototype(4, 1, "DevOps Specialist to lead our infrastructure transformation"),
            CreatePrototype(5, 1, "Junior Developer opportunity with mentorship program"),

            // Company & Culture (ClassificationId = 2)
            CreatePrototype(6, 2, "We're a flat organization where every voice matters and ideas can come from anyone"),
            CreatePrototype(7, 2, "Our culture emphasizes work-life balance with flexible hours and remote work options"),
            CreatePrototype(8, 2, "We offer competitive benefits including health insurance, pension plans, and wellness grants"),
            CreatePrototype(9, 2, "Join a diverse and inclusive team that values collaboration and continuous learning"),
            CreatePrototype(10, 2, "Modern office in the city center with great coffee, snacks, and regular team events"),

            // Must-Haves (ClassificationId = 3)
            CreatePrototype(11, 3, "Minimum 3 years of professional development experience required"),
            CreatePrototype(12, 3, "Must have strong communication skills and be fluent in English"),
            CreatePrototype(13, 3, "Bachelor's degree in Computer Science or equivalent practical experience"),
            CreatePrototype(14, 3, "Proven track record of delivering complex software projects on time"),
            CreatePrototype(15, 3, "Experience with agile methodologies and version control systems"),

            // Technologies (ClassificationId = 4)
            CreatePrototype(16, 4, "Experience with C# and .NET Core / .NET 6+ is essential"),
            CreatePrototype(17, 4, "Strong knowledge of React, TypeScript, and modern JavaScript"),
            CreatePrototype(18, 4, "Proficiency with SQL databases and Entity Framework Core"),
            CreatePrototype(19, 4, "Experience with cloud platforms (Azure/AWS) and containerization (Docker)"),
            CreatePrototype(20, 4, "Familiarity with microservices architecture and message queues"),

            // Responsibilities (ClassificationId = 5)
            CreatePrototype(21, 5, "Design and implement new features while maintaining existing codebase"),
            CreatePrototype(22, 5, "Participate in code reviews and provide constructive feedback to peers"),
            CreatePrototype(23, 5, "Collaborate with product managers to refine requirements and estimate work"),
            CreatePrototype(24, 5, "Write unit tests and ensure high code quality standards"),
            CreatePrototype(25, 5, "Mentor junior developers and contribute to technical documentation"),

            // Location (ClassificationId = 6)
            CreatePrototype(26, 6, "Stockholm office with hybrid work model (2-3 days per week in office)"),
            CreatePrototype(27, 6, "Fully remote position within Sweden with quarterly team meetups"),
            CreatePrototype(28, 6, "Gothenburg based role with flexible working hours"),
            CreatePrototype(29, 6, "Malmö office, relocation assistance available for the right candidate"),
            CreatePrototype(30, 6, "Remote-first company with optional co-working space access"),

            // Application Process (ClassificationId = 7)
            CreatePrototype(31, 7, "Apply by March 31st with your CV and a brief cover letter"),
            CreatePrototype(32, 7, "Process includes: initial screening, interview, and team meet"),
            CreatePrototype(33, 7, "We review continuously, so don't wait to apply"),
            CreatePrototype(35, 7, "Two interview rounds: HR screening followed by technical panel")
        };

        context.Prototypes.AddRange(prototypes);
    }

    private static Prototype CreatePrototype(int id, int classificationId, string text)
    {
        var embedding = _jinaService.GenerateEmbeddingFloat(text);

        return new Prototype
        {
            Id = id,
            ClassificationId = classificationId,
            Text = text,
            Embedding = embedding.SelectMany(BitConverter.GetBytes).ToArray()
        };
    }
}