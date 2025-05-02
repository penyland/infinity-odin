using Microsoft.EntityFrameworkCore;

namespace Odin.WorkerService;

internal class WorkerDbContext : DbContext
{
    public WorkerDbContext(DbContextOptions<WorkerDbContext> options) : base(options)
    {
    }

    public DbSet<WorkerModel> Workers { get; set; } = null!;
}
