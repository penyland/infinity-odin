using ModelContextProtocol.Server;
using Odin.Features.MeetupPlanner.Infrastructure;
using System.ComponentModel;
using System.Text.Json;

namespace MeetupPlanner;

[McpServerToolType]
public class MeetupPlannerMcpTools(IMeetupPlannerDb meetupPlannerDb)
{
    private readonly IMeetupPlannerDb meetupPlannerDb = meetupPlannerDb;

    [McpServerTool, Description("Get a list of locations where meetups have been or will be")]
    public async Task<string> GetLocationsAsync()
    {
        var locations = await meetupPlannerDb.GetLocationsAsync();
        return JsonSerializer.Serialize(locations, MeetupPlannerContext.Default.ListLocation);
    }

    [McpServerTool, Description("Get a list of locations where meetups have been or will be in a given city")]
    public async Task<string> GetLocationsByCityAsync([Description("The name of the city")] string city)
    {
        var locations = await meetupPlannerDb.GetLocationsByCityAsync(city);
        return JsonSerializer.Serialize(locations, MeetupPlannerContext.Default.ListLocation);
    }

    [McpServerTool, Description("Get a list of locations where meetups have been or will be with a given name")]
    public async Task<List<Location>> GetLocationsByNameAsync(string name)
    {
        return await meetupPlannerDb.GetLocationByNameAsync(name);
    }
}
