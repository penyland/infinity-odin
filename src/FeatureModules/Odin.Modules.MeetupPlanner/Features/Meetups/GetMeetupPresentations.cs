using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Meetups;

public static class GetMeetupPresentations
{
    public sealed record Query(Guid MeetupId);

    public sealed record Response(IReadOnlyCollection<PresentationDto> Presentations);

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

                var response = presentations.Select(p => new PresentationDto(
                p.PresentationId,
                p.Title,
                p.Abstract,
                [.. p.PresentationSpeakers
                    .Where(ps => ps.Speaker != null)
                    .Select(ps => ps.Speaker)
                    .Select(s => new SpeakerDto(
                        s.SpeakerId,
                        s.FullName,
                        s.Company,
                        s.TwitterUrl,
                        s.GitHubUrl,
                        s.LinkedInUrl,
                        s.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio))]));

                return Result.Success(new Response([.. response]));
            }
            catch (Exception ex)
            {
                return Result.Failure<Response>(ex);
            }
        }
    }
}
