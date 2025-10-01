namespace Odin.Modules.MeetupPlanner.Features.Common;

public record SpeakerDto
{
    public Guid SpeakerId { get; init; }
    public string FullName { get; init; }
    public string? Company { get; init; }
    public string? TwitterUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? Bio { get; init; }
}
