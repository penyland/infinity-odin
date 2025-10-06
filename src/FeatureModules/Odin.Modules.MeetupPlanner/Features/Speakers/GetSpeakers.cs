using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Speakers;

public static class GetSpeakers
{
    public sealed record Query();

    public sealed record Response(IReadOnlyList<SpeakerDto> Speakers);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Response>
    {
        public async Task<Result<Response>> HandleAsync(CancellationToken cancellationToken)
        {
            try
            {
                var speakers = await dbContext.Speakers
                    .Include(s => s.Bios)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);
                var response = speakers.Select(s => new SpeakerDto
                {
                    SpeakerId = s.SpeakerId,
                    FullName = s.FullName,
                    ThumbnailUrl = s.ThumbnailUrl
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
