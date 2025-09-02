using Infinity.Toolkit.FeatureModules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        context.Services.AddDbContext<MeetupPlannerDbContext>(options => options.UseSqlServer());

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

        app.MapGet("/locations2", async (MeetupPlannerDbContext dbContext) =>
        {
            var locations = await dbContext.GetAllLocationsAsync();
            return Results.Ok(locations);
        });

        app.MapGet("/locations3", async (MeetupPlannerDbContext dbContext) =>
        {
            var locations = await dbContext.Locations.AsNoTracking().ToListAsync();
            return Results.Ok(locations);
        });
    }
}

internal class MeetupPlannerDbContext(IOptions<DatabaseConnectionOptions> options) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(options.Value.MeetupPlanner);
    }

    public async Task<List<Location>> GetAllLocationsAsync()
    {
        var result = await Database.SqlQuery<Location>(
            $"SELECT * FROM dbo.Locations ORDER BY [Name]")
            .AsNoTracking()
            .ToListAsync();

        return result;
    }

    public DbSet<Location> Locations { get; set; }
}
