namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class MeetupSpeaker : Entity
{
    public Guid MeetupId { get; set; }

    public Guid SpeakerId { get; set; }

    public string Role { get; set; }

    public virtual Meetup Meetup { get; set; }

    public virtual Speaker Speaker { get; set; }
}
