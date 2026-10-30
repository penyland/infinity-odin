using TUnit.AspNetCore;

namespace Odin.Api.IntegrationTests;

public class IntegrationTestClassFixture : TestWebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        return base.CreateHost(builder);
    }
}
