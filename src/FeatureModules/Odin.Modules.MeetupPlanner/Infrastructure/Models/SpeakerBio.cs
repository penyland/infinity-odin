namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class SpeakerBio : Entity
{
    public Guid SpeakerBioId { get; set; }
    public Guid SpeakerId { get; set; }
    public string Title { get; set; }
    public string Bio { get; set; }
    public bool IsPrimary { get; set; }

    public Speaker Speaker { get; set; }
}
