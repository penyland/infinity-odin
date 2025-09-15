namespace Odin.Features.MeetupPlanner.Models2;
public sealed class Meetup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string TimeZone { get; set; } = "Europe/Stockholm";
    public MeetupStatus Status { get; set; } = MeetupStatus.Draft;
    public int? Capacity { get; set; }

    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public string? OrganizerNotes { get; set; }
    public bool RsvpOpen { get; set; }
    public DateTimeOffset? RsvpDeadlineUtc { get; set; }

    public ICollection<MeetupSpeaker> MeetupSpeakers { get; set; } = new List<MeetupSpeaker>();
    public ICollection<ScheduleSlot> Schedule { get; set; } = new List<ScheduleSlot>();
    public ICollection<Link> Links { get; set; } = new List<Link>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();

    public DateTimeOffset CreatedUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = "Sweden";
    public string? Room { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public int? Capacity { get; set; }
    public string? AccessNotes { get; set; }
    public string? SponsorName { get; set; }
    public string? SponsorLogoUrl { get; set; }

    public ICollection<Meetup> Meetups { get; set; } = new List<Meetup>();
}

public sealed class Speaker
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? GitHub { get; set; }
    public string? LinkedIn { get; set; }
    public string? Bio { get; set; }
    public string? HeadshotUrl { get; set; }
    public string[] PrimaryTopics { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = "Confirmed"; // could become enum if fixed list

    public Guid? ResponsibleOrganizerId { get; set; }
    public Organizer? ResponsibleOrganizer { get; set; }

    public ICollection<PresentationSpeaker> PresentationSpeakers { get; set; } = new List<PresentationSpeaker>();
    public ICollection<MeetupSpeaker> MeetupSpeakers { get; set; } = new List<MeetupSpeaker>();
    public ICollection<SpeakerAvailability> Availability { get; set; } = new List<SpeakerAvailability>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
}

public sealed class Presentation
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Abstract { get; set; }
    public PresentationStatus Status { get; set; } = PresentationStatus.Proposed;
    public int? DurationMinutes { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Guid OwnerOrganizerId { get; set; }
    public Organizer OwnerOrganizer { get; set; } = null!;
    public ICollection<PresentationSpeaker> Speakers { get; set; } = new List<PresentationSpeaker>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

public sealed class PresentationSpeaker
{
    public Guid PresentationId { get; set; }
    public Presentation Presentation { get; set; } = null!;
    public Guid SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public SpeakerEngagementRole Role { get; set; } = SpeakerEngagementRole.Presenter;
}

public sealed class MeetupSpeaker
{
    public Guid MeetupId { get; set; }
    public Meetup Meetup { get; set; } = null!;
    public Guid SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = null!;
    public Guid? PresentationId { get; set; }
    public Presentation? Presentation { get; set; }
    public SpeakerEngagementRole Role { get; set; } = SpeakerEngagementRole.Presenter;
}

public sealed class ScheduleSlot
{
    public Guid Id { get; set; }
    public Guid MeetupId { get; set; }
    public Meetup Meetup { get; set; } = null!;
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public SlotType Type { get; set; } = SlotType.Presentation;
    public Guid? PresentationId { get; set; }
    public Presentation? Presentation { get; set; }
    public Guid? RelatedPresentationId { get; set; }
    public Presentation? RelatedPresentation { get; set; }
    public string? Title { get; set; }
    public int SortOrder { get; set; }
}

public sealed class SpeakerAvailability
{
    public Guid Id { get; set; }
    public Guid SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = null!;
    public DateOnly? UnavailableDate { get; set; }
    public string? PreferredMonth { get; set; }
}

public sealed class Feedback
{
    public Guid Id { get; set; }
    public Guid? MeetupId { get; set; }
    public Meetup? Meetup { get; set; }
    public Guid? PresentationId { get; set; }
    public Presentation? Presentation { get; set; }
    public Guid? SpeakerId { get; set; }
    public Speaker? Speaker { get; set; }
    public Guid UserId { get; set; }  // referencing User
    public int Rating { get; set; }   // 1..5
    public string? Comment { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class Vote
{
    public Guid PresentationId { get; set; }
    public Presentation Presentation { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public int Value { get; set; } = 1; // upvote pattern
}

public sealed class Link
{
    public Guid Id { get; set; }
    public Guid MeetupId { get; set; }
    public Meetup Meetup { get; set; } = null!;
    public string Type { get; set; } = null!; // public, internal-planning, recording, slides
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
}

public sealed class Organizer : User
{
    public ICollection<Speaker> SpeakersResponsibleFor { get; set; } = new List<Speaker>();
    public ICollection<Presentation> OwnedPresentations { get; set; } = new List<Presentation>();
}

public class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string AuthSubject { get; set; } = null!; // external provider subject
    public bool Active { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public sealed class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!; // e.g. "Meetups.Read"
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
