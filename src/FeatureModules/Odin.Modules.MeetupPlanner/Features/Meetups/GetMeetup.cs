using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Modules.MeetupPlanner.Features.Common;
using Odin.Modules.MeetupPlanner.Infrastructure;

namespace Odin.Modules.MeetupPlanner.Features.Meetups;

public static class GetMeetup
{
    public sealed record Query(Guid MeetupId);

    public sealed record Response(MeetupDto Meetup);

    internal class Handler(MeetupPlannerContext dbContext) : IRequestHandler<Query, Response>
    {
        public async Task<Result<Response>> HandleAsync(IHandlerContext<Query> context, CancellationToken cancellationToken)
        {
            try
            {
                var meetup = await dbContext.Meetups
                    .Include(m => m.Location)
                    .Include(m => m.ScheduleSlots)
                    .ThenInclude(s => s.Presentation)
                    .ThenInclude(p => p.PresentationSpeakers)
                    .ThenInclude(ps => ps.Speaker)
                    .ThenInclude(sb => sb.Bios)
                    .AsNoTracking()
                    .Where(m => m.MeetupId == context.Request.MeetupId)
                .Select(m => new MeetupDto(m.MeetupId,
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
                            Name = m.Location.Name,
                            Description = m.Location.Description,
                            City = m.Location.City,
                            Country = m.Location.Country,
                            LocationId = m.Location.LocationId,
                            PostalCode = m.Location.PostalCode,
                            Street = m.Location.Street,
                            IsActive = m.Location.IsActive
                        },
                        m.ScheduleSlots
                            .Where(slot => slot.Presentation != null)
                            .Select(slot => slot.Presentation)
                            .Select(p => new PresentationDto
                            {
                                PresentationId = p.PresentationId,
                                Title = p.Title,
                                Abstract = p.Abstract,
                                Speakers = p.PresentationSpeakers
                                    .Select(ps => ps.Speaker)
                                    .Select(s => new SpeakerDto
                                    {
                                        SpeakerId = s.SpeakerId,
                                        FullName = s.FullName,
                                    }).ToList()
                            }).ToList()))
                    .FirstOrDefaultAsync(cancellationToken);

                return meetup == null ? Result.Failure<Response>("No meetup found") : Result.Success(new Response(meetup));
            }
            catch (Exception ex)
            {
                return Result.Failure<Response>(ex);
            }
        }
    }
}
