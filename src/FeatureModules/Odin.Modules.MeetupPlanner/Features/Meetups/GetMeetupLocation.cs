using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Meetups;

public static class GetMeetupLocation
{
    public sealed record Query(Guid MeetupId);

    public sealed record Response(LocationDetailedDto Location);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken)
        {
            try
            {
                var meetup = await dbContext.Meetups
                    .Include(m => m.Location)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MeetupId == context.Request.MeetupId, cancellationToken: cancellationToken);

                if (meetup == null || meetup.Location == null)
                {
                    return Result.Failure<Response>("No meetup found");
                }

                var location = meetup.Location;
                var response = new LocationDetailedDto
                {
                    LocationId = location.LocationId,
                    Name = location.Name,
                    Street = location.Street,
                    City = location.City,
                    PostalCode = location.PostalCode,
                    Country = location.Country,
                    Description = location.Description,
                    MaxCapacity = location.MaxCapacity,
                    IsActive = location.IsActive
                };

                return Result.Success(new Response(response));
            }
            catch (Exception ex)
            {
                return Result.Failure<Response>(ex);
            }
        }
    }
}
