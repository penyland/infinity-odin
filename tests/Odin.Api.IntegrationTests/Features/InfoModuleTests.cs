using static Odin.Api.Features.Info.InfoEndpoints;

namespace Odin.Api.IntegrationTests.Features;

[ClassDataSource<IntegrationTestClassFixture>(Shared = SharedType.PerClass)]
public class InfoModuleTests(IntegrationTestClassFixture factory)
{
    [Test]
    public async Task GetVersion_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/info/version", CancellationToken.None);
        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Test]
    public async Task GetVersion_ReturnsExpectedMediaType()
    {
        // Arrange
        var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/info/version", CancellationToken.None);
        // Assert
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
    }

    [Test]
    public async Task GetInfo_ReturnsExpectedResponse()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetFromJsonAsync<Info>("/info/version", CancellationToken.None);

        // Assert
        await Assert.That(response).IsNotNull();
    }
}
