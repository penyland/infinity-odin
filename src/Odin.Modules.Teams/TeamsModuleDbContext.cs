using Microsoft.EntityFrameworkCore;

namespace Odin.Modules.Teams;

internal class TeamsModuleDbContext : DbContext
{
    public DbSet<PullRequests> PullRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PullRequests>()
            .Property(b => b.PullRequestId)
            .IsRequired();
    }
}

public sealed class PullRequests
{
    public int Id { get; set; }
    public int PullRequestId { get; set; }
    public int RepositoryId { get; set; }
    public int TeamsChannelId { get; set; }
    public int TeamsMessageId { get; set; }
    public string CardJson { get; set; } = string.Empty;
}
