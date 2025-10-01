namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public partial class Location : Entity
{
    public Guid LocationId { get; set; }

    public string Name { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string PostalCode { get; set; }

    public string Country { get; set; }

    public int? MaxCapacity { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public ICollection<Meetup> Meetups { get; set; } = [];
}

