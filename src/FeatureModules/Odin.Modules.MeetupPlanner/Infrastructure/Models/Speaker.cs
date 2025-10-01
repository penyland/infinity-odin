namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class Speaker : Entity
{
    public Guid SpeakerId { get; set; }
    public string FullName { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? BlogUrl { get; set; }
    public string? ThumbnailUrl { get; set; }

    public ICollection<SpeakerBio> Bios { get; set; }
    public ICollection<PresentationSpeaker> PresentationSpeakers { get; set; }
    public ICollection<MeetupSpeaker> MeetupSpeakers { get; set; }
}
