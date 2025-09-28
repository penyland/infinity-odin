using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Features.MeetupPlanner.Models;

namespace Odin.Features.MeetupPlanner.GetMeetups;

internal class GetMeetupsHandler(MeetupPlannerContext dbContext) : IRequestHandler<IReadOnlyCollection<MeetupDto>>
{
    public async Task<Result<IReadOnlyCollection<MeetupDto>>> HandleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var meetups = await dbContext.Meetups
                .Include(m => m.Location)
                .OrderBy(m => m.StartUtc)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);

            var getMeetupsResponse = new GetMeetupsResponse
            {
                Meetups = meetups.Select(meetup => new MeetupDto(
                meetup.MeetupId,
                meetup.Title,
                meetup.Description,
                meetup.StartUtc,
                meetup.EndUtc,
                new RsvpDto(
                    meetup.TotalSpots ?? 0,
                    meetup.RsvpYesCount ?? 0,
                    meetup.RsvpNoCount ?? 0,
                    meetup.RsvpWaitlistCount ?? 0,
                    meetup.AttendanceCount ?? 0),
                new LocationDto(
                    meetup.Location.LocationId,
                    meetup.Location.Name,
                    meetup.Location.Street,
                    meetup.Location.City,
                    meetup.Location.PostalCode,
                    meetup.Location.Country,
                    meetup.Location.Description
                    ),
                []))

                //[.. presentations.Select(p => new PresentationDto(
                //    p.PresentationId,
                //    p.Title,
                //    p.Abstract,
                //    [.. p.PresentationSpeakers
                //        .Select(ps => ps.Speaker)
                //        .Where(s => s != null)
                //        .Select(s => new SpeakerDto(
                //            s.SpeakerId,
                //            s.FullName,
                //            s.Company,
                //            s.TwitterUrl,
                //            s.GitHubUrl,
                //            s.LinkedInUrl,
                //            s.Bios.FirstOrDefault(b => b.IsPrimary)?.Bio
                //            ))
                //        ]))
                //]);
            };

            return Result.Success<IReadOnlyCollection<MeetupDto>>([.. getMeetupsResponse.Meetups]);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyCollection<MeetupDto>>(ex.Message);
        }
    }
}

internal class GetMeetupsResponse
{
    public IEnumerable<MeetupDto> Meetups { get; set; }
}
