using JobTracker.Application.Features.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Application.Infrastructure.Data;

public static class SeedData
{
    public static void Initialize(IDbContextFactory<AppDbContext> factory)
    {
        using var context = factory.CreateDbContext();

        if (context.JobTrackers.Any())
            return;

        context.JobTrackers.AddRange(
            new Features.JobTracker.JobTracker { Keyword = "utvecklare" },
            new Features.JobTracker.JobTracker { Keyword = "developer"}
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
}