using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.InMemory;

namespace Odin.Api;

public class ApiModule : IWebFeatureModule
{
    public IModuleInfo? ModuleInfo { get; }

    public void MapEndpoints(WebApplication app)
    {
    }

    public ModuleContext RegisterModule(ModuleContext context)
    {
        context.Services.AddInfinityMessaging()
            .ConfigureInMemoryBus(builder => { });

        return context;
    }
}
