using Odin.Modules.MeetupPlanner.Features.Common;

namespace Odin.Modules.MeetupPlanner.Features.Meetups;

public record MeetupDto(
    Guid MeetupId,
    string Title,
    string Description,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    RsvpDto Rsvp,
    LocationDto Location,
    IReadOnlyList<PresentationDto>? Presentations = null
);

public record RsvpDto(
    int TotalSpots,
    int RsvpYesCount,
    int RsvpNoCount,
    int RsvpWaitlistCount,
    int AttendanceCount
    );
