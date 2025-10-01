namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class PresentationSpeaker : Entity
{
    public Guid PresentationId { get; set; }

    public Guid SpeakerId { get; set; }

    public bool IsPrimary { get; set; }

    public virtual Presentation Presentation { get; set; }

    public virtual Speaker Speaker { get; set; }
}
