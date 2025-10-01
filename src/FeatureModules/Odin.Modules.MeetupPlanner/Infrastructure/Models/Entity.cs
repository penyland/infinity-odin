using System.Text.Json.Serialization;

namespace Odin.Modules.MeetupPlanner.Infrastructure.Models;

public class Entity
{
    [JsonIgnore]
    public DateTimeOffset CreatedUtc { get; set; }

    [JsonIgnore]
    public string CreatedBy { get; set; }

    [JsonIgnore]
    public DateTimeOffset UpdatedUtc { get; set; }

    [JsonIgnore]
    public string UpdatedBy { get; set; }
}
