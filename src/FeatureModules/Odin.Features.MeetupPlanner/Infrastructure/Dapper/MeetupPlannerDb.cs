using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Odin.Features.MeetupPlanner.Models;
using System.Data;
using System.Text.Json.Serialization;

namespace Odin.Features.MeetupPlanner.Infrastructure.Dapper;

public class DatabaseConnectionOptions
{
    public string MeetupPlanner { get; set; } = string.Empty;
}

public interface IMeetupPlannerDb
{
    Task<string> AddLocationAsync(Location location, CancellationToken cancellationToken = default);
    IDbConnection CreateOpenConnection();
    Task<List<Location>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<List<Location>> GetLocationByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Location>> GetLocationsByCityAsync(string city, CancellationToken cancellationToken = default);
    Task<Location> GetLocationByIdAsync(Guid locationId, CancellationToken cancellationToken = default);
}

public class MeetupPlannerDb(IOptions<DatabaseConnectionOptions> options) : IMeetupPlannerDb
{
    private readonly string connectionString = options.Value.MeetupPlanner;

    private SqlConnection CreateConnection() => new(connectionString);

    public IDbConnection CreateOpenConnection()
    {
        var conn = CreateConnection();
        conn.Open();
        return conn; // caller disposes
    }

    public async Task<string> AddLocationAsync(Location location, CancellationToken cancellationToken = default)
    {
        location.LocationId = Guid.NewGuid();
        location.CreatedUtc = DateTimeOffset.UtcNow;

        var sql = """
            INSERT INTO dbo.Locations (LocationId, Name, Street, City, PostalCode, Country)
            VALUES (@LocationId, @Name, @Street, @City, @PostalCode, @Country)
            """;

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, location);

        return location.LocationId.ToString();
    }

    public async Task<List<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT * FROM dbo.Locations
            ORDER BY [Name]
            """;

        using var conn = CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var result = await conn.QueryAsync<Location>(sql);
        return [.. result];
    }

    public async Task<List<Location>> GetLocationsByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT * FROM dbo.Locations
            WHERE City = @City
            """;
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Location>(sql, new { City = city });
        return [.. result];
    }

    public async Task<Location> GetLocationByIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT * FROM dbo.Locations
            WHERE LocationId = @LocationId
            """;

        using var conn = CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<Location>(sql, new { LocationId = locationId });

        return result ?? throw new KeyNotFoundException($"Location with ID {locationId} not found.");
    }

    public async Task<List<Location>> GetLocationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT * FROM dbo.Locations
            WHERE Name = @Name
            """;

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Location>(sql, new { Name = name });
        return [.. result];
    }
}

//public record Location
//{
//    public Guid LocationId { get; set; }
//    public string Name { get; set; } = string.Empty;
//    public string Street { get; set; } = string.Empty;
//    public string City { get; set; } = string.Empty;
//    public string PostalCode { get; set; } = string.Empty;
//    public string Country { get; set; } = "SE";

//    [JsonIgnore]
//    public string? CreatedBy { get; init; }

//    [JsonIgnore]
//    public DateTimeOffset? CreatedUtc { get; set; }

//    [JsonIgnore]
//    public string? UpdatedBy { get; init; }

//    [JsonIgnore]
//    public DateTimeOffset? UpdatedUtc { get; init; }
//}

[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(List<Location>))]
public sealed partial class MeetupPlannerDapperContext : JsonSerializerContext
{
}
