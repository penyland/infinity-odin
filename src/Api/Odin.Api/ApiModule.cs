using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.InMemory;

namespace Odin.Api;

[WebFeatureModule("Odin.Api", "1.0.0")]
public class ApiModule : WebFeatureModule
{
    public override void RegisterModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddInfinityMessaging()
            .ConfigureInMemoryBus(builder => { });
    }

    public override void MapEndpoints(WebApplication app)
    {
    }
}
