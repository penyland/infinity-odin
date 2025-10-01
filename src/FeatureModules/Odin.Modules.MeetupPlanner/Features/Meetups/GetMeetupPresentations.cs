using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Meetups;

public static class GetMeetupPresentations
{
    public sealed record Query(Guid MeetupId);

    public sealed record Response(IReadOnlyList<PresentationDto> Presentations);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken)
        {
            try
            {
                var presentations = await dbContext.ScheduleSlots
                    .Where(s => s.MeetupId == context.Request.MeetupId && s.Presentation != null)
                    .Include(s => s.Presentation)
                    .ThenInclude(p => p.PresentationSpeakers)
                    .ThenInclude(ps => ps.Speaker)
                    .ThenInclude(sb => sb.Bios)
                    .Select(s => s.Presentation)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);

                if (presentations == null || presentations.Count == 0)
                {
                    return Result.Failure<Response>(
                        new Error("400", "No presentations found for the specified meetup."));
                }

                var response = presentations.Select(p => new PresentationDto
                {
                    PresentationId = p.PresentationId,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    Speakers = [.. p.PresentationSpeakers
                        .Select(ps => ps.Speaker)
                        .Select(s => new SpeakerDto
                        {
                            SpeakerId = s.SpeakerId,
                            FullName = s.FullName,
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
