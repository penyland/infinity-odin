using Infinity.Toolkit.FeatureModules;
using Microsoft.AspNetCore.Builder;
using System.Reflection;

namespace Odin.Features.MeetupPlanner;

public class MeetupPlannerModule : IWebFeatureModule
{
    public IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo(typeof(MeetupPlannerModule).FullName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public ModuleContext RegisterModule(ModuleContext context)
    {
        // Register services, controllers, etc. for the Meetup Planner feature here.
        return context;
    }

    public void MapEndpoints(WebApplication app)
    {
        throw new NotImplementedException();
    }
}
