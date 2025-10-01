using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Meetups;

public record GetMeetupsRequest(string? Status);

public record GetMeetupsResponse(IReadOnlyCollection<MeetupDto> Meetups);

internal class GetMeetups(MeetupPlannerContext dbContext) : IRequestHandler<GetMeetupsRequest, GetMeetupsResponse>
{
    public async Task<Result<GetMeetupsResponse>> HandleAsync(IHandlerContext<GetMeetupsRequest> context, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.Meetups
                .Include(m => m.Location)
                .Include(m => m.ScheduleSlots)
                .ThenInclude(s => s.Presentation)
                .ThenInclude(p => p.PresentationSpeakers)
                .ThenInclude(ps => ps.Speaker)
                .OrderBy(m => m.StartUtc)
                .AsQueryable();

            if (!string.IsNullOrEmpty(context.Request.Status))
            {
                query = query.Where(e => e.Status == context.Request.Status);
            }

            var meetups = await query
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);

            var getMeetupsResponse = meetups.Select(m => new MeetupDto(
                m.MeetupId,
                m.Title,
                m.Description,
                m.StartUtc,
                m.EndUtc,
                new RsvpDto(
                    m.TotalSpots ?? 0,
                    m.RsvpYesCount ?? 0,
                    m.RsvpNoCount ?? 0,
                    m.RsvpWaitlistCount ?? 0,
                    m.AttendanceCount ?? 0),
                new LocationDto
                {
                    LocationId = m.Location.LocationId,
                    Name = m.Location.Name
                },
                [.. m.ScheduleSlots.Select(s => new PresentationDto(
                    s.Presentation.PresentationId,
                    s.Presentation.Title,
                    Speakers: [.. s.Presentation.PresentationSpeakers
                        .Select(ps => ps.Speaker)
                        .Select(s => new SpeakerDto(
                            s.SpeakerId,
                            s.FullName
                            ))]
                    )).ToList()]));

            return Result.Success<GetMeetupsResponse>(new GetMeetupsResponse([.. getMeetupsResponse]));
        }
        catch (Exception ex)
        {
            return Result.Failure<GetMeetupsResponse>(ex.Message);
        }
    }

}
