using Infinity.Toolkit;
using Infinity.Toolkit.AspNetCore;
using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Odin.Modules.MeetupPlanner.Extensions;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Features.Locations;
using Odin.Modules.MeetupPlanner.Features.Meetups;
using Odin.Modules.MeetupPlanner.Features.Speakers;
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

        builder.Services.AddRequestHandler<GetPresentations.Response, GetPresentations.Handler>();

        builder.Services.AddRequestHandler<GetSpeakers.Response, GetSpeakers.Handler>();
        builder.Services.AddRequestHandler<GetSpeaker.Query, GetSpeaker.Response, GetSpeaker.Handler>();
        builder.Services.AddRequestHandler<GetSpeakerBios.Query, GetSpeakerBios.Response, GetSpeakerBios.Handler>();
        builder.Services.AddRequestHandler<GetSpeakerPresentations.Query, GetSpeakerPresentations.Response, GetSpeakerPresentations.Handler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/meetupplanner")
            .WithTags("Meetup Planner");

        group.MapGet("/locations", async (IRequestHandler<GetLocations.Response> handler) =>
        {
            var response = await handler.HandleAsync();
            return response is Failure ?
                Results.Problem(response.ToProblemDetails()) :
                Results.Json(response.Value.Locations);
        })
        .Produces<IReadOnlyList<LocationDto>>(200);

        group.MapGetQuery<GetLocation.Query, GetLocation.Response>("/locations/{locationId}")
            .Produces<LocationDto>()
            .Produces(400);

        group.MapGet("/meetups", async (IRequestHandler<GetMeetups.Query, GetMeetups.Response> handler, [AsParameters] MeetupQueryParameters queryParams) =>
        {
            var meetupStatus = MeetupStatus.All;
            if (queryParams.Status is not null && !queryParams.TryParseStatus(out meetupStatus))
            {
                return Results.BadRequest($"Invalid status '{queryParams.Status}'");
            }

            var response = await handler.HandleAsync(new HandlerContext<GetMeetups.Query> { Request = new GetMeetups.Query(meetupStatus.ToString()) });

            return response is Failure ?
                Results.Problem(response.ToProblemDetails()) :
                Results.Json(response.Value.Meetups);
        })
        .Produces<IReadOnlyList<MeetupDto>>(200)
        .Produces(400);

        group.MapGetQuery<GetMeetup.Query, GetMeetup.Response>("/meetups/{meetupId}")
            .Produces<MeetupDto>()
            .Produces(400);

        group.MapGetQuery<GetMeetupLocation.Query, GetMeetupLocation.Response>("/meetups/{meetupId}/location")
            .Produces<LocationDto>()
            .Produces(400);

        group.MapGetQuery<GetMeetupPresentations.Query, GetMeetupPresentations.Response>("/meetups/{meetupId}/presentations")
            .Produces<PresentationDto>()
            .Produces(400);

        group.MapGetQuery<GetMeetupRsvps.Query, GetMeetupRsvps.Response>("/meetups/{meetupId}/rsvps")
            .Produces<RsvpDto>()
            .Produces(400);

        group.MapGetQuery<GetPresentations.Response>("/presentations")
            .Produces<IReadOnlyList<PresentationDto>>(200)
            .Produces(400);

        group.MapGetQuery<GetSpeakers.Response>("/speakers")
            .Produces<IReadOnlyList<SpeakerDto>>(200)
            .Produces(400);

        group.MapGetQuery<GetSpeaker.Query, GetSpeaker.Response>("/speakers/{speakerId}")
            .Produces<SpeakerDto>()
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGetQuery<GetSpeakerBios.Query, GetSpeakerBios.Response>("/speakers/{speakerId}/bios")
            .Produces<IReadOnlyList<SpeakerBioDto>>(200)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGetQuery<GetSpeakerPresentations.Query, GetSpeakerPresentations.Response>("/speakers/{speakerId}/presentations")
            .Produces<IReadOnlyList<PresentationDto>>(200)
            .Produces(StatusCodes.Status400BadRequest);
    }
}

// Model for query parameters
public class MeetupQueryParameters
{
    public string? Status { get; set; }

    public bool TryParseStatus(out MeetupStatus statusEnum)
    {
        return Enum.TryParse(Status, true, out statusEnum);
    }
}
