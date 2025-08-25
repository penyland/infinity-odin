using Infinity.Toolkit.FeatureModules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Odin.Features.MeetupPlanner.Infrastructure;
using System.Reflection;

namespace Odin.Features.MeetupPlanner;

public class MeetupPlannerModule : IWebFeatureModule
{
    public IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo(typeof(MeetupPlannerModule).FullName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public ModuleContext RegisterModule(ModuleContext context)
    {
        context.Services.Configure<DatabaseConnectionOptions>(context.Configuration.GetSection("ConnectionStrings"));
        context.Services.AddSingleton<IMeetupPlannerDb, MeetupPlannerDb>();

        return context;
    }

    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("/locations", async (IMeetupPlannerDb database, [FromQuery] string? city, [FromQuery] string? name) =>
        {
            var hasCity = !string.IsNullOrWhiteSpace(city);
            var hasName = !string.IsNullOrWhiteSpace(name);

            if (hasCity && hasName)
            {
                var cityResults = await database.GetLocationsByCityAsync(city!);
                return cityResults.Where(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            if (hasCity)
            {
                return await database.GetLocationsByCityAsync(city!);
            }

            if (hasName)
            {
                return await database.GetLocationByNameAsync(name!);
            }

            return await database.GetLocationsAsync();
        });

        app.MapPost("/locations", async (IMeetupPlannerDb database, [FromBody] Location location) =>
        {
            // Add validation as needed

            await database.AddLocationAsync(location);
            return Results.Created($"/location/{location.LocationId}", location);
        });
    }
}
