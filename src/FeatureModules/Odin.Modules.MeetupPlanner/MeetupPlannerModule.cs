using Infinity.Toolkit;
using Infinity.Toolkit.AspNetCore;
using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Features.Locations;
using Odin.Modules.MeetupPlanner.Features.Meetups;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner;

public class MeetupPlannerModule : WebFeatureModule
{
    public override void RegisterModule(WebApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<MeetupPlannerContext>("MeetupPlanner");

        builder.Services.AddRequestHandler<GetLocations.Response, GetLocations.Handler>();
        builder.Services.AddRequestHandler<GetLocation.Query, GetLocation.Response, GetLocation.Handler>();

        builder.Services.AddRequestHandler<GetMeetups.Query, GetMeetups.Response, GetMeetups.Handler>();
        builder.Services.AddRequestHandler<GetMeetup.Query, GetMeetup.Response, GetMeetup.Handler>();
        builder.Services.AddRequestHandler<GetMeetupLocation.Query, GetMeetupLocation.Response, GetMeetupLocation.Handler>();
        builder.Services.AddRequestHandler<GetMeetupPresentations.Query, GetMeetupPresentations.Response, GetMeetupPresentations.Handler>();
        builder.Services.AddRequestHandler<GetMeetupRsvps.Query, GetMeetupRsvps.Response, GetMeetupRsvps.Handler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/meetupplanner")
            .WithTags("Meetup Planner");

        group.MapGet("/locations", async(IRequestHandler<GetLocations.Response > handler) =>
        {
            var response = await handler.HandleAsync();
            return response is Failure ?
                Results.Problem(response.ToProblemDetails()) :
                Results.Json(response.Value.Locations);
        })
        .Produces<IReadOnlyCollection<LocationDto>>(200);

        group.MapGetQuery<GetLocation.Query, GetLocation.Response>("/locations/{locationId}");

        group.MapGet("/meetups", async (IRequestHandler<GetMeetups.Query, GetMeetups.Response> handler, [FromQuery] string? status) =>
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

            var response = await handler.HandleAsync(new HandlerContext<GetMeetups.Query> { Request = new GetMeetups.Query(status) });

            return response is Failure ?
                Results.Problem(response.ToProblemDetails()) :
                Results.Json(response.Value.Meetups);
        })
        .Produces<IReadOnlyCollection<MeetupDto>>(200);

        group.MapGetQuery<GetMeetup.Query, GetMeetup.Response>("/meetups/{meetupId}")
            .Produces<MeetupDto>();

        group.MapGetQuery<GetMeetupLocation.Query, GetMeetupLocation.Response>("/meetups/{meetupId}/location")
            .Produces<LocationDto>();

        group.MapGetQuery<GetMeetupPresentations.Query, GetMeetupPresentations.Response>("/meetups/{meetupId}/presentations")
            .Produces<GetMeetupPresentations.Response>();

        group.MapGetQuery<GetMeetupRsvps.Query, GetMeetupRsvps.Response>("/meetups/{meetupId}/rsvps")
            .Produces<GetMeetupRsvps.Response>();

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




public enum EventStatus
{
    Proposed,
    Scheduled,
    Completed,
    Cancelled
}

// Model for query parameters
public class EventQueryParameters
{
    public string? Status { get; set; }

    public bool TryParseStatus(out EventStatus statusEnum)
    {
        return Enum.TryParse(Status, true, out statusEnum);
    }
}
