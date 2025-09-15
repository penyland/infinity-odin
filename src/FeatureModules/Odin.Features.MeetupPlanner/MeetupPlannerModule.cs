using Infinity.Toolkit.FeatureModules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Odin.Features.MeetupPlanner.Infrastructure.Dapper;
using Odin.Features.MeetupPlanner.Models;
using System.Reflection;

namespace Odin.Features.MeetupPlanner;

public class MeetupPlannerModule : IWebFeatureModule
{
    public IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo(typeof(MeetupPlannerModule).FullName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public ModuleContext RegisterModule(ModuleContext context)
    {
        context.Services.Configure<DatabaseConnectionOptions>(context.Configuration.GetSection("ConnectionStrings"));
        context.Services.AddSingleton<IMeetupPlannerDb, MeetupPlannerDb>();

        //context.Services.AddDbContext<MeetupPlannerDbContext>(options => options.UseSqlServer());
        context.Services.AddDbContext<MeetupPlannerContext>(options => options.UseSqlServer(context.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")));

        return context;
    }

    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/meetupplanner")
            .WithTags("Meetup Planner");

        group.MapGet("/dapper/locations", async (IMeetupPlannerDb database, [FromQuery] string? city, [FromQuery] string? name) =>
        {
            var hasCity = !string.IsNullOrWhiteSpace(city);
            var hasName = !string.IsNullOrWhiteSpace(name);

            if (hasCity && hasName)
            {
                var cityResults = await database.GetLocationsByCityAsync(city!);
                return cityResults.Where(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            if (hasCity)
            {
                return await database.GetLocationsByCityAsync(city!);
            }

            if (hasName)
            {
                return await database.GetLocationByNameAsync(name!);
            }

            return await database.GetLocationsAsync();
        });

        //group.MapPost("/dapper/locations", async (IMeetupPlannerDb database, [FromBody] Location location) =>
        //{
        //    // Add validation as needed

        //    await database.AddLocationAsync(location);
        //    return Results.Created($"/location/{location.LocationId}", location);
        //});

        group.MapGet("/locations", async (MeetupPlannerContext dbContext) =>
        {
            var locations = await dbContext.Locations.AsNoTracking().ToListAsync();

            return Results.Json(locations);
        });

        group.MapGet("/locations/{locationId}", async (MeetupPlannerContext dbContext, Guid locationId) =>
        {
            var location = await dbContext.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LocationId == locationId);
            return location != null ? Results.Json(location) : Results.NotFound();
        });

        group.MapGet("/meetups", async (MeetupPlannerContext dbContext) =>
        {
            //var meetups = await dbContext.Meetups.AsNoTracking().ToListAsync();
            var meetups = await dbContext.Meetups
                .Include(m => m.Location)
                .AsNoTracking()
                .ToListAsync();

            var response = meetups.Select(m => new
            {
                m.MeetupId,
                m.Title,
                m.Description,
                m.StartUtc,
                m.EndUtc,
                Rsvp = new
                {
                    TotalSpots = m.TotalSpots ?? 0,
                    RsvpYesCount = m.RsvpYesCount ?? 0,
                    RsvpNoCount = m.RsvpNoCount ?? 0,
                    RsvpWaitlistCount = m.RsvpWaitlistCount ?? 0,
                    AttendanceCount = m.AttendanceCount ?? 0
                },
                Location = new
                {
                    m.Location.LocationId,
                    m.Location.Name,
                    m.Location.Street,
                    m.Location.City,
                    m.Location.PostalCode,
                    m.Location.Country,
                    m.Location.Description
                }
            });

            return Results.Ok(response);
        });

        group.MapGet("/meetups/{meetupId}", async (MeetupPlannerContext dbContext, Guid meetupId) =>
        {
            var meetup = await dbContext.Meetups
                .Include(m => m.Location)
                .Include(m => m.ScheduleSlots)
                .ThenInclude(s => s.Presentation)
                .ThenInclude(p => p.PresentationSpeakers)
                .ThenInclude(ps => ps.Speaker)
                .ThenInclude(sb => sb.Bios)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MeetupId == meetupId);

            if (meetup == null)
            {
                return Results.NotFound();
            }

            var presentations = meetup.ScheduleSlots
                .Where(slot => slot.Presentation != null)
                .Select(slot => slot.Presentation)
                .ToList();

            var response = new MeetupDto(
                meetup.MeetupId,
                meetup.Title,
                meetup.Description,
                meetup.StartUtc,
                meetup.EndUtc,
                new RsvpDto(
                    meetup.TotalSpots ?? 0,
                    meetup.RsvpYesCount ?? 0,
                    meetup.RsvpNoCount ?? 0,
                    meetup.RsvpWaitlistCount ?? 0,
                    meetup.AttendanceCount ?? 0),
                new LocationDto(
                    meetup.Location.LocationId,
                    meetup.Location.Name,
                    meetup.Location.Street,
                    meetup.Location.City,
                    meetup.Location.PostalCode,
                    meetup.Location.Country,
                    meetup.Location.Description
                    ),
                [.. presentations.Select(p => new PresentationDto(
                    p.PresentationId,
                    p.Title,
                    p.Abstract,
                    [.. p.PresentationSpeakers
                        .Select(ps => ps.Speaker)
                        .Where(s => s != null)
                        .Select(s => new SpeakerDto(
                            s.SpeakerId,
                            s.FullName,
                            s.Company,
                            s.TwitterUrl,
                            s.GitHubUrl,
                            s.LinkedInUrl,
                            s.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio
                            ))
                        ]))
                ]);

            return response != null ? Results.Json(response) : Results.NotFound();
        });

        group.MapGet("/presentations", async (MeetupPlannerContext dbContext) =>
        {
            var presentations = await dbContext.Presentations
                .Include(p => p.PresentationSpeakers)
                .ThenInclude(ps => ps.Speaker)
                .AsNoTracking()
                .ToListAsync();
            var response = presentations.Select(p => new
            {
                p.PresentationId,
                p.Title,
                p.Abstract,
                Speakers = p.PresentationSpeakers
                    .Where(ps => ps.Speaker != null)
                    .Select(ps => new
                    {
                        ps.Speaker.SpeakerId,
                        ps.Speaker.FullName,
                        ps.Speaker.Company,
                        ps.Speaker.TwitterUrl,
                        ps.Speaker.GitHubUrl,
                        ps.Speaker.LinkedInUrl
                    })
            });
            return Results.Ok(response);
        });

        group.MapGet("/speakers", async (MeetupPlannerContext dbContext) =>
        {
            var speakers = await dbContext.Speakers
                .Include(s => s.Bios)
                .AsNoTracking()
                .ToListAsync();
            var response = speakers.Select(s => new
            {
                s.SpeakerId,
                s.FullName,
                s.Company,
                s.TwitterUrl,
                s.GitHubUrl,
                s.LinkedInUrl,
                s.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio
            });
            return Results.Ok(response);
        });
    }
}

internal class MeetupPlannerDbContext(IOptions<DatabaseConnectionOptions> options) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(options.Value.MeetupPlanner);
    }

    public async Task<List<Location>> GetAllLocationsAsync()
    {
        var result = await Database.SqlQuery<Location>(
            $"SELECT * FROM dbo.Locations ORDER BY [Name]")
            .AsNoTracking()
            .ToListAsync();

        return result;
    }

    public DbSet<Location> Locations { get; set; }
}

public record MeetupDto(
    Guid MeetupId,
    string Title,
    string Description,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    RsvpDto Rsvp,
    LocationDto Location,
    List<PresentationDto> Presentations
);

public record PresentationDto(
    Guid PresentationId,
    string Title,
    string Abstract,
    List<SpeakerDto> Speakers
);

public record SpeakerDto(
    Guid SpeakerId,
    string FullName,
    string? Company,
    string? TwitterUrl,
    string? GitHubUrl,
    string? LinkedInUrl,
    string? Bio
);

public record LocationDto(
    Guid LocationId,
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country,
    string Description
);

public record RsvpDto(
    int TotalSpots,
    int RsvpYesCount,
    int RsvpNoCount,
    int RsvpWaitlistCount,
    int AttendanceCount
    );
