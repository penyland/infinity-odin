namespace Odin.Modules.AzureDevOps.PullRequests.Models;

internal partial record GitUser
{
    public string Id { get; init; }

    public string DisplayName { get; init; }

    public string UniqueName { get; init; }

    public string Url { get; init; }

    public string ImageUrl { get; init; }
}

internal partial record GitUser
{
    public static implicit operator Contracts.Models.GitUser(GitUser gitUser)
    {
        return new Contracts.Models.GitUser
        {
            Id = gitUser.Id,
            DisplayName = gitUser.DisplayName,
            UniqueName = gitUser.UniqueName,
            Url = gitUser.Url,
            ImageUrl = gitUser.ImageUrl
        };
    }
}
