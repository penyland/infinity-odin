using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Speakers;

public static class GetSpeaker
{
    public sealed record Query(Guid SpeakerId);

    public sealed record Response(SpeakerDto Speaker);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken = default)
        {
            try
            {
                var speaker = await dbContext.Speakers
                    .Include(s => s.Bios)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SpeakerId == context.Request.SpeakerId, cancellationToken: cancellationToken);

                if (speaker == null)
                {
                    return Result.Failure<Response>($"Speaker with ID {context.Request.SpeakerId} not found.");
                }

                var response = new SpeakerDto
                {
                    SpeakerId = speaker.SpeakerId,
                    FullName = speaker.FullName,
                    Company = speaker.Company,
                    TwitterUrl = speaker.TwitterUrl,
                    GitHubUrl = speaker.GitHubUrl,
                    LinkedInUrl = speaker.LinkedInUrl,
                    Bio = speaker.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio
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
