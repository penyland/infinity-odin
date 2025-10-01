using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Speakers;

public static class GetSpeakerBios
{
    public sealed record Query(Guid SpeakerId);

    public sealed record Response(IReadOnlyList<SpeakerBioDto> SpeakerBios);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken = default)
        {
            try
            {
                var bios = await dbContext.SpeakerBios
                    .Where(b => b.SpeakerId == context.Request.SpeakerId)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);

                if (bios == null || bios.Count == 0)
                {
                    return Result.Failure<Response>(bios == null
                        ? $"Speaker with ID {context.Request.SpeakerId} not found."
                        : $"No bios found for speaker with ID {context.Request.SpeakerId}.");
                }

                var response = bios.Select(b => new SpeakerBioDto
                {
                    SpeakerBioId = b.SpeakerBioId,
                    Bio = b.Bio,
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

public record SpeakerBioDto
{
    public Guid SpeakerBioId { get; init; }
    public Guid SpeakerId { get; init; }
    public string Bio { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
