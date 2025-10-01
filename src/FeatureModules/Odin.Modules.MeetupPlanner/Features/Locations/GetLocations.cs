using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Locations;

public static class GetLocations
{
    public sealed record Response(IReadOnlyList<LocationDto> Locations);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Response>
    {
        public async Task<Result<Response>> HandleAsync(CancellationToken cancellationToken)
        {
            try
            {
                var locations = await dbContext.Locations.AsNoTracking().ToListAsync(cancellationToken: cancellationToken);

                var locationDtos = locations.Select(l => new LocationDto
                {
                    LocationId = l.LocationId,
                    Name = l.Name,
                    MaxCapacity = l.MaxCapacity,
                    IsActive = l.IsActive,
                });

                return Result.Success(new Response([.. locationDtos]));
            }
            catch (Exception ex)
            {
                return Result.Failure<Response>(ex);
            }
        }
    }
}
