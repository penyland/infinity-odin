namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class ScheduleSlot : Entity
{
    public Guid SlotId { get; set; }

    public Guid MeetupId { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset? StartUtc { get; set; }

    public DateTimeOffset? EndUtc { get; set; }

    public Guid? PresentationId { get; set; }

    public string Title { get; set; }

    public virtual Meetup Meetup { get; set; }

    public virtual Presentation Presentation { get; set; }
}
