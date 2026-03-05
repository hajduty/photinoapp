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
            new Classification { Id = 1, Name = "Responsibilities", Color = "#2F3746" }, // muted slate
            new Classification { Id = 2, Name = "Requirements",     Color = "#4A2A22" }, // dark burnt orange
            new Classification { Id = 3, Name = "Technologies",     Color = "#2A3550" }, // deep navy
            new Classification { Id = 4, Name = "The Offer",        Color = "#4A243A" }, // dark muted pink
        };

        context.Classifications.AddRange(classificationData);

        var prototypes = new List<Prototype>
        {
            // 1. Responsibilities 
            CreatePrototype(1,  1, "Design and implement new product features end-to-end"),
            CreatePrototype(2,  1, "Participate in code reviews and mentor junior developers"),
            CreatePrototype(3,  1, "Write automated tests and maintain code quality standards"),
            CreatePrototype(4,  1, "Collaborate with product and design to define requirements"),
            CreatePrototype(5,  1, "Own and improve CI/CD pipelines and deployment processes"),
            CreatePrototype(6,  1, "What you will be working on day-to-day"),
            CreatePrototype(7,  1, "Your responsibilities and duties in this role"),
            CreatePrototype(8,  1, "Designa och implementera nya produktfunktioner"),
            CreatePrototype(9,  1, "Delta i kodgranskning och mentorskap av juniora utvecklare"),
            CreatePrototype(10, 1, "Skriva automatiserade tester och upprätthålla kodkvalitet"),
            CreatePrototype(11, 1, "Samarbeta med produkt och design för att ta fram krav"),
            CreatePrototype(12, 1, "Dina arbetsuppgifter och ansvarsområden i rollen"),
        
            // 2. Requirements 
            CreatePrototype(13, 2, "3+ years of professional software development experience"),
            CreatePrototype(14, 2, "Bachelor's degree in Computer Science or equivalent"),
            CreatePrototype(15, 2, "Fluent English, both written and spoken"),
            CreatePrototype(16, 2, "Must have experience with distributed systems"),
            CreatePrototype(17, 2, "Required: strong understanding of OOP principles"),
            CreatePrototype(18, 2, "Minimum qualifications and candidate requirements"),
            CreatePrototype(19, 2, "If you are an Android developer: minimum 3 years experience required"),
            CreatePrototype(20, 2, "We are looking for someone with a proven track record"),
            CreatePrototype(21, 2, "Minst 3 års erfarenhet av professionell mjukvaruutveckling"),
            CreatePrototype(22, 2, "Flytande svenska och engelska i tal och skrift"),
            CreatePrototype(23, 2, "Krav: god förståelse för objektorienterad programmering"),
            CreatePrototype(24, 2, "Vi söker någon med dokumenterad erfarenhet"),
            CreatePrototype(25, 2, "Meriterande om du har erfarenhet av distribuerade system"),
        
            // 3. Technologies
            CreatePrototype(26, 3, "C# / .NET Core and Entity Framework"),
            CreatePrototype(27, 3, "React, TypeScript, and modern frontend tooling"),
            CreatePrototype(28, 3, "Azure, AWS, Docker, Kubernetes"),
            CreatePrototype(29, 3, "PostgreSQL, Redis, RabbitMQ, REST APIs"),
            CreatePrototype(30, 3, "Python, FastAPI, NumPy, Pandas"),
            CreatePrototype(31, 3, "Tech stack: Node.js, GraphQL, MongoDB"),
            CreatePrototype(32, 3, "Experience with the following technologies is required"),
            CreatePrototype(33, 3, "Vi använder följande tekniker i vår stack"),
            CreatePrototype(34, 3, "Erfarenhet av nedanstående teknologier är ett krav"),
        
            // 4. The Offer
            CreatePrototype(35, 4, "Fully remote within Sweden, hybrid Stockholm 2–3 days"),
            CreatePrototype(36, 4, "30 days vacation, stock options, and learning budget"),
            CreatePrototype(37, 4, "Flat organization where every voice matters"),
            CreatePrototype(38, 4, "Mission-driven team that values work-life balance"),
            CreatePrototype(39, 4, "We are a Series B startup building developer tooling"),
            CreatePrototype(40, 4, "Competitive salary and benefits package"),
            CreatePrototype(41, 4, "About us and why you should join our team"),
            CreatePrototype(42, 4, "On-site in Berlin, relocation support provided"),
            CreatePrototype(43, 4, "Hybridarbete från Stockholmskontoret 2–3 dagar i veckan"),
            CreatePrototype(44, 4, "30 dagars semester, friskvårdsbidrag och tjänstepension"),
            CreatePrototype(45, 4, "Vi är ett snabbväxande bolag med platt organisation"),
            CreatePrototype(46, 4, "Om oss och varför du ska välja att jobba hos oss"),
            CreatePrototype(47, 4, "Konkurrenskraftig lön och förmåner"),
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