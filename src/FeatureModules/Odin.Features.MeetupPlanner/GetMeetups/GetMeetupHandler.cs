using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Features.MeetupPlanner.Models;

namespace Odin.Features.MeetupPlanner.GetMeetups;

public record GetMeetupFromIdRequest(Guid MeetupId);

internal class GetMeetupHandler(MeetupPlannerContext dbContext) : IRequestHandler<GetMeetupFromIdRequest, MeetupDto>
{
    public async Task<Result<MeetupDto>> HandleAsync(IHandlerContext<GetMeetupFromIdRequest> context, CancellationToken cancellationToken)
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
                    new LocationDto(
                        m.Location.LocationId,
                        m.Location.Name,
                        m.Location.Street,
                        m.Location.City,
                        m.Location.PostalCode,
                        m.Location.Country,
                        m.Location.Description
                        ),
                        m.ScheduleSlots
                            .Where(slot => slot.Presentation != null)
                            .Select(slot => slot.Presentation)
                            .Select(p => new PresentationDto(
                            p.PresentationId,
                            p.Title,
                            p.Abstract,
                            p.PresentationSpeakers
                                .Select(ps => ps.Speaker)
                                .Where(s => s != null)
                                    .Select(s => new SpeakerDto(
                                        s.SpeakerId,
                                        s.FullName,
                                        s.Company,
                                        s.TwitterUrl,
                                        s.GitHubUrl,
                                        s.LinkedInUrl,
                                        s.Bios.First(b => b.IsPrimary).Bio)).ToList()))
                            .ToList()))
                .FirstOrDefaultAsync(cancellationToken);


            return meetup == null ? Result.Failure<MeetupDto>("No meetup found") : Result.Success(meetup);
        }
        catch (Exception ex)
        {
            return Result.Failure<MeetupDto>(ex);
        }
    }
}
