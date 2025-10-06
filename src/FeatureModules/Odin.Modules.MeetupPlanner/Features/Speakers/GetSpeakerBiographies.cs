using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Speakers;

public static class GetSpeakerBiographies
{
    public sealed record Query(Guid SpeakerId);

    public sealed record Response(IReadOnlyList<SpeakerBiographyDto> SpeakerBiographies);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken = default)
        {
            try
            {
                var biographies = await dbContext.SpeakerBios
                    .Where(b => b.SpeakerId == context.Request.SpeakerId)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);

                if (biographies == null || biographies.Count == 0)
                {
                    return Result.Failure<Response>(biographies == null
                        ? $"Speaker with ID {context.Request.SpeakerId} not found."
                        : $"No biographies found for speaker with ID {context.Request.SpeakerId}.");
                }

                var response = biographies.Select(b => new SpeakerBiographyDto
                {
                    SpeakerBiographyId = b.SpeakerBioId,
                    Biography = b.Bio,
                    IsPrimary = b.IsPrimary
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

public record SpeakerBiographyDto
{
    public Guid SpeakerBiographyId { get; init; }
    public Guid SpeakerId { get; init; }
    public string Biography { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
