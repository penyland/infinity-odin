using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.InMemory;

namespace Odin.Api;

public class ApiModule : WebFeatureModule
{
    public override void RegisterModule(WebApplicationBuilder builder)
    {
        builder.Services.AddInfinityMessaging()
            .ConfigureInMemoryBus(imbBuilder => { });
    } 
}
