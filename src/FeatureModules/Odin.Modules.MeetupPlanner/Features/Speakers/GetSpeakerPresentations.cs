using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Speakers;

public static class GetSpeakerPresentations
{
    public sealed record Query(Guid SpeakerId);

    public sealed record Response(IReadOnlyList<PresentationDto> Presentations);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken = default)
        {
            try
            {
                var presentations = await dbContext.PresentationSpeakers
                    .Where(ps => ps.SpeakerId == context.Request.SpeakerId)
                    .Include(ps => ps.Presentation)
                    .AsNoTracking()
                    .Select(ps => ps.Presentation)
                    .ToListAsync(cancellationToken);

                if (presentations == null || presentations.Count == 0)
                {
                    return Result.Failure<Response>(presentations == null
                        ? $"Speaker with ID {context.Request.SpeakerId} not found."
                        : $"No presentations found for speaker with ID {context.Request.SpeakerId}.");
                }

                var response = presentations.Select(p => new PresentationDto
                {
                    PresentationId = p.PresentationId,
                    Title = p.Title,
                    Abstract = p.Abstract
                }).ToList();

                return Result.Success(new Response(response));
            }
            catch (Exception ex)
            {
                return Result.Failure<Response>(ex);
            }
        }
    }
}
