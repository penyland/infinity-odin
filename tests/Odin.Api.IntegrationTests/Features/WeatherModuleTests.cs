using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Odin.Api.Features.Weather;

namespace Odin.Api.IntegrationTests.Features;

public class WeatherModuleTests(IntegrationTestClassFixture factory) : IClassFixture<IntegrationTestClassFixture>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task GetWeatherForecast_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/weatherforecast");
        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsExpectedMediaType()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/weatherforecast");
        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsExpectedResponse()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/weatherforecast");
        // Assert
        var forecast = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        forecast.Should().NotBeNullOrEmpty();
        forecast.Should().HaveCount(5);
    }
}
