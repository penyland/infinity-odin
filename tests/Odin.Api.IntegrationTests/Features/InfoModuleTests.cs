using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using static Odin.Api.Features.Info.InfoEndpoints;

namespace Odin.Api.IntegrationTests.Features;

public class InfoModuleTests(IntegrationTestClassFixture factory) : IClassFixture<IntegrationTestClassFixture>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task GetVersion_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/info/version", CancellationToken.None);
        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetVersion_ReturnsExpectedMediaType()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/info/version", CancellationToken.None);
        // Assert
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task GetInfo_ReturnsExpectedResponse()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetFromJsonAsync<Info>("/info/version", CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }
}
