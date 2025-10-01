namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class Presentation : Entity
{
    public Guid PresentationId { get; set; }
    public string Title { get; set; }
    public string Abstract { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; }
    public string? RepoUrl { get; set; }
    public string? SlidesUrl { get; set; }

    public ICollection<PresentationSpeaker> PresentationSpeakers { get; set; }
    public ICollection<ScheduleSlot> ScheduleSlots { get; set; }
}
