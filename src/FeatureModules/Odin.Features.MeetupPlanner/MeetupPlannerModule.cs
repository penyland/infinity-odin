using Infinity.Toolkit;
using Infinity.Toolkit.AspNetCore;
using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Odin.Features.MeetupPlanner.Handlers.GetMeetups;
using Odin.Features.MeetupPlanner.Infrastructure.Dapper;
using Odin.Features.MeetupPlanner.Models;

namespace Odin.Features.MeetupPlanner;

public class MeetupPlannerModule : WebFeatureModule
{
    public override void RegisterModule(WebApplicationBuilder builder)
    {
        builder.Services.Configure<DatabaseConnectionOptions>(builder.Configuration.GetSection("ConnectionStrings"));
        builder.Services.AddSingleton<IMeetupPlannerDb, MeetupPlannerDb>();

        builder.AddSqlServerDbContext<MeetupPlannerContext>("MeetupPlanner");

        builder.Services.AddRequestHandler<GetMeetupsRequest, GetMeetupsResponse, GetMeetupsHandler>();
        builder.Services.AddRequestHandler<GetMeetupFromIdRequest, GetMeetupFromIdResponse, GetMeetupHandler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/meetupplanner")
            .WithTags("Meetup Planner");

        //group.MapGet("/dapper/locations", async (IMeetupPlannerDb database, [FromQuery] string? city, [FromQuery] string? name) =>
        //{
        //    var hasCity = !string.IsNullOrWhiteSpace(city);
        //    var hasName = !string.IsNullOrWhiteSpace(name);

        //    if (hasCity && hasName)
        //    {
        //        var cityResults = await database.GetLocationsByCityAsync(city!);
        //        return cityResults.Where(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        //    }

        //    if (hasCity)
        //    {
        //        return await database.GetLocationsByCityAsync(city!);
        //    }

        //    if (hasName)
        //    {
        //        return await database.GetLocationByNameAsync(name!);
        //    }

        //    return await database.GetLocationsAsync();
        //});

        group.MapGet("/locations", async (MeetupPlannerContext dbContext) =>
        {
            var locations = await dbContext.Locations.AsNoTracking().ToListAsync();

            // Map to LocationDTO
            var response = locations.Select(l => new LocationDto(
                l.LocationId,
                l.Name,
                l.Street,
                l.City,
                l.PostalCode,
                l.Country,
                l.Description));

            return Results.Json(response);
        });

        group.MapGet("/locations/{locationId}", async (MeetupPlannerContext dbContext, Guid locationId) =>
        {
            var location = await dbContext.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LocationId == locationId);

            var response = location != null ? new LocationDto(
                location.LocationId,
                location.Name,
                location.Street,
                location.City,
                location.PostalCode,
                location.Country,
                location.Description) : null;

            return response != null ? Results.Json(response) : Results.NotFound(locationId);
        });

        group.MapGet("/meetups", async (IRequestHandler<GetMeetupsRequest, GetMeetupsResponse> handler, [FromQuery] string? status) =>
        {
            if (status is not null)
            {
                // Validate status
                var valid = status.ToLowerInvariant() switch
                {
                    "proposed" or
                    "scheduled" or
                    "completed" or
                    "cancelled" => true,
                    _ => false
                };

                if (!valid)
                {
                    return Results.BadRequest();
                }
            }

            var response = await handler.HandleAsync(new HandlerContext<GetMeetupsRequest> { Request = new GetMeetupsRequest(status) });

            return response is Failure ?
                Results.Problem(response.ToProblemDetails()) :
                Results.Json(response.Value.Meetups);
        })
        .Produces<IReadOnlyCollection<MeetupDto>>(200);

        group.MapGetQuery<GetMeetupFromIdRequest, GetMeetupFromIdResponse>("/meetups/{meetupId}")
            .Produces<MeetupDto>()
            .Produces(400);

        group.MapGet("/meetups/{meetupId}/location", async (MeetupPlannerContext dbContext, Guid meetupId) =>
        {
            var meetup = await dbContext.Meetups
                .Include(m => m.Location)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MeetupId == meetupId);
            if (meetup == null || meetup.Location == null)
            {
                return Results.NotFound();
            }
            var location = meetup.Location;
            var response = new LocationDto(
                location.LocationId,
                location.Name,
                location.Street,
                location.City,
                location.PostalCode,
                location.Country,
                location.Description);
            return Results.Ok(response);
        });

        // GetPresentationsFromMeetupId
        group.MapGet("/meetups/{meetupId}/presentations", async (MeetupPlannerContext dbContext, Guid meetupId) =>
        {
            var presentations = await dbContext.ScheduleSlots
                .Where(s => s.MeetupId == meetupId && s.Presentation != null)
                .Include(s => s.Presentation)
                .ThenInclude(p => p.PresentationSpeakers)
                .ThenInclude(ps => ps.Speaker)
                .ThenInclude(sb => sb.Bios)
                .AsNoTracking()
                .Select(s => s.Presentation)
                .ToListAsync();
            if (presentations == null || presentations.Count == 0)
            {
                return Results.NotFound();
            }
            var response = presentations.Select(p => new PresentationDto(
                p.PresentationId,
                p.Title,
                p.Abstract,
                [.. p.PresentationSpeakers
                    .Where(ps => ps.Speaker != null)
                    .Select(ps => ps.Speaker)
                    .Select(s => new SpeakerDto(
                        s.SpeakerId,
                        s.FullName,
                        s.Company,
                        s.TwitterUrl,
                        s.GitHubUrl,
                        s.LinkedInUrl,
                        s.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio))]));
            return Results.Ok(response);
        });

        group.MapGet("/meetups/{meetupId}/rsvps", async (MeetupPlannerContext dbContext, Guid meetupId) =>
        {
            var meetup = await dbContext.Meetups
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MeetupId == meetupId);

            if (meetup == null)
            {
                return Results.NotFound();
            }

            var rsvp = new RsvpDto(
                meetup.TotalSpots ?? 0,
                meetup.RsvpYesCount ?? 0,
                meetup.RsvpNoCount ?? 0,
                meetup.RsvpWaitlistCount ?? 0,
                meetup.AttendanceCount ?? 0);

            return Results.Ok(rsvp);
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

        group.MapGet("/speakers/{speakerId}", async (MeetupPlannerContext dbContext, Guid speakerId) =>
        {
            var speaker = await dbContext.Speakers
                .Include(s => s.Bios)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SpeakerId == speakerId);
            if (speaker == null)
            {
                return Results.NotFound();
            }
            var response = new
            {
                speaker.SpeakerId,
                speaker.FullName,
                speaker.Company,
                speaker.TwitterUrl,
                speaker.GitHubUrl,
                speaker.LinkedInUrl,
                speaker.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio
            };
            return Results.Ok(response);
        });

        group.MapGet("/speakers/{speakerId}/bios", async (MeetupPlannerContext dbContext, Guid speakerId) =>
        {
            var bios = await dbContext.SpeakerBios
                .Where(b => b.SpeakerId == speakerId)
                .AsNoTracking()
                .ToListAsync();
            if (bios == null || bios.Count == 0)
            {
                return Results.NotFound();
            }
            var response = bios.Select(b => new
            {
                b.SpeakerBioId,
                b.Bio,
                b.IsPrimary
            });
            return Results.Ok(response);
        });

        group.MapGet("/speakers/{speakerId}/presentations", async (MeetupPlannerContext dbContext, Guid speakerId) =>
        {
            var presentations = await dbContext.PresentationSpeakers
                .Where(ps => ps.SpeakerId == speakerId)
                .Include(ps => ps.Presentation)
                .AsNoTracking()
                .Select(ps => ps.Presentation)
                .ToListAsync();

            if (presentations == null || presentations.Count == 0)
            {
                return Results.NotFound();
            }
            var response = presentations.Select(p => new PresentationDto(
                p.PresentationId,
                p.Title,
                p.Abstract,
                null));
            return Results.Ok(response);
        });
    }
}

public record MeetupDto(
    Guid MeetupId,
    string Title,
    string Description,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    RsvpDto Rsvp,
    LocationDto Location,
    List<PresentationDto>? Presentations = null
);

public record PresentationDto(
    Guid PresentationId,
    string Title,
    string? Abstract = null,
    List<SpeakerDto>? Speakers = null
);

public record SpeakerDto(
    Guid SpeakerId,
    string FullName,
    string? Company = null,
    string? TwitterUrl = null,
    string? GitHubUrl = null,
    string? LinkedInUrl = null,
    string? Bio = null
);

public record LocationDto(
    Guid LocationId,
    string Name,
    string? Street = null,
    string? City = null,
    string? PostalCode = null,
    string? Country = null,
    string? Description = null
);

public record RsvpDto(
    int TotalSpots,
    int RsvpYesCount,
    int RsvpNoCount,
    int RsvpWaitlistCount,
    int AttendanceCount
    );
