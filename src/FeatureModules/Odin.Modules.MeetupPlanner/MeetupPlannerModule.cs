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
        builder.Services.AddRequestHandler<GetSpeakerBiographies.Query, GetSpeakerBiographies.Response, GetSpeakerBiographies.Handler>();
        builder.Services.AddRequestHandler<GetSpeakerPresentations.Query, GetSpeakerPresentations.Response, GetSpeakerPresentations.Handler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/meetupplanner")
            .WithTags("Meetup Planner");

        group.MapGetHandler<GetLocations.Response, IReadOnlyList<LocationDto>>("/locations", map => map.Locations);

        group.MapGetHandler<GetLocation.Query, GetLocation.Response, LocationDetailedDto>("/locations/{locationId}", map => map.Location);

        group.MapGet("/meetups", async (IRequestHandler<GetMeetups.Query, GetMeetups.Response> handler, [AsParameters] MeetupQueryParameters queryParams) =>
        {
            var meetupStatus = MeetupStatus.All;
            if (queryParams.Status is not null && !queryParams.TryParseStatus(out meetupStatus))
            {
                return Results.BadRequest($"Invalid status '{queryParams.Status}'");
            }

            var response = await handler.HandleAsync(new HandlerContext<GetMeetups.Query> { Request = new GetMeetups.Query(meetupStatus) });

            return response is Failure ?
                TypedResults.Problem(response.ToProblemDetails()) :
                TypedResults.Json(response.Value.Meetups);
        })
        .Produces<IReadOnlyList<MeetupDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGetHandler<GetMeetup.Query, GetMeetup.Response, MeetupDto>("/meetups/{meetupId}", map => map.Meetup);

        group.MapGetHandler<GetMeetupLocation.Query, GetMeetupLocation.Response, LocationDetailedDto>("/meetups/{meetupId}/location", map => map.Location);

        group.MapGetHandler<GetMeetupPresentations.Query, GetMeetupPresentations.Response, IReadOnlyList<PresentationDto>>("/meetups/{meetupId}/presentations", map => map.Presentations);

        group.MapGetHandler<GetMeetupRsvps.Query, GetMeetupRsvps.Response, RsvpDto>("/meetups/{meetupId}/rsvps", map => map.Rsvp);

        group.MapGetHandler<GetPresentations.Response, IReadOnlyList<PresentationDto>>("/presentations", map => map.Presentations);

        group.MapGetHandler<GetSpeakers.Response, IReadOnlyList<SpeakerDto>>("/speakers", map => map.Speakers);

        group.MapGetHandler<GetSpeaker.Query, GetSpeaker.Response, SpeakerDetailedDto>("/speakers/{speakerId}", map => map.Speaker);

        group.MapGetHandler<GetSpeakerBiographies.Query, GetSpeakerBiographies.Response, IReadOnlyList<SpeakerBiographyDto>>("/speakers/{speakerId}/biographies", map => map.SpeakerBiographies);

        group.MapGetHandler<GetSpeakerPresentations.Query, GetSpeakerPresentations.Response, IReadOnlyList<PresentationDto>>("/speakers/{speakerId}/presentations", map => map.Presentations);
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
