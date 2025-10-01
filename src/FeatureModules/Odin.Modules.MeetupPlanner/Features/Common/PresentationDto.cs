namespace Odin.Modules.MeetupPlanner.Features.Common;

public record PresentationDto
{
    public Guid PresentationId { get; init; }
    public string Title { get; init; }
    public string? Abstract { get; init; }
    public IReadOnlyList<SpeakerDto>? Speakers { get; init; }
}
