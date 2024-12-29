using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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
        var response = await client.GetAsync("/info/version");
        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetVersion_ReturnsExpectedMediaType()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/info");
        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetInfo_ReturnsExpectedResponse()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/info");
        // Assert
        var forecast = await response.Content.ReadFromJsonAsync<Info[]>();
        forecast.Should().NotBeNullOrEmpty();
    }
}
