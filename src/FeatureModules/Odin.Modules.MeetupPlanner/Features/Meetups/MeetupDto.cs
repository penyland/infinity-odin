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

public record RsvpDto(
    int TotalSpots,
    int RsvpYesCount,
    int RsvpNoCount,
    int RsvpWaitlistCount,
    int AttendanceCount
    );
