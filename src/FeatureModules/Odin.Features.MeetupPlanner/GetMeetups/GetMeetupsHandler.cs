using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Microsoft.EntityFrameworkCore;
using Odin.Features.MeetupPlanner.Models;

namespace Odin.Features.MeetupPlanner.GetMeetups;

public record GetMeetupsRequest(string? Status);

public record GetMeetupsResponse(IReadOnlyCollection<MeetupDto> Meetups);

internal class GetMeetupsHandler(MeetupPlannerContext dbContext) : IRequestHandler<GetMeetupsRequest, GetMeetupsResponse>
{
    public async Task<Result<GetMeetupsResponse>> HandleAsync(IHandlerContext<GetMeetupsRequest> context, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = dbContext.Meetups
                .Include(m => m.Location)
                .OrderBy(m => m.StartUtc)
                .AsQueryable();

            if (!string.IsNullOrEmpty(context.Request.Status))
            {
                query = query.Where(e => e.Status == context.Request.Status);
            }

            var meetups = await query
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);

            var getMeetupsResponse = meetups.Select(meetup => new MeetupDto(
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
                []));

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
            //};

            return Result.Success<GetMeetupsResponse>(new GetMeetupsResponse([.. getMeetupsResponse]));
        }
        catch (Exception ex)
        {
            return Result.Failure<GetMeetupsResponse>(ex.Message);
        }
    }    
}
