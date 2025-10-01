namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class Meetup : Entity
{
    public Guid MeetupId { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public DateTimeOffset StartUtc { get; set; }

    public DateTimeOffset EndUtc { get; set; }

    public Guid LocationId { get; set; }

    public string Status { get; set; }

    public int? TotalSpots { get; set; }

    public int? RsvpYesCount { get; set; }

    public int? RsvpNoCount { get; set; }

    public int? RsvpWaitlistCount { get; set; }

    public int? AttendanceCount { get; set; }

    public virtual Location Location { get; set; }

    public virtual ICollection<MeetupSpeaker> MeetupSpeakers { get; set; } = [];

    public virtual ICollection<ScheduleSlot> ScheduleSlots { get; set; } = [];
}
