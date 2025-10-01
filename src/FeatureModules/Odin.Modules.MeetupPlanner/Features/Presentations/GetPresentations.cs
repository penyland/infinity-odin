using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Locations;

public static class GetPresentations
{
    public sealed record Query();

    public sealed record Response(IReadOnlyList<PresentationDto> Presentations);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Response>
    {
        public async Task<Result<Response>> HandleAsync(CancellationToken cancellationToken)
        {
            try
            {
                var presentations = await dbContext.Presentations
                    .Include(p => p.PresentationSpeakers)
                    .ThenInclude(ps => ps.Speaker)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);
                var response = presentations.Select(p => new PresentationDto
                {
                    PresentationId = p.PresentationId,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    Speakers = [.. p.PresentationSpeakers
                    .Select(ps => new SpeakerDto
                    {
                         SpeakerId = ps.Speaker.SpeakerId,
                         FullName = ps.Speaker.FullName,
                    })]
                });

                return Result.Success(new Response([.. response]));
            }
            catch (Exception ex)
            {
                return Result.Failure<Response>(ex);
            }
        }
    }
}
