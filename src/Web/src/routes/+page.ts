import { api } from '$lib/api/client';

export const load = async () => {
  try {
    // Fetch all meetups and speakers in parallel
    const [allMeetups, speakers, locations] = await Promise.all([
      api.meetups.getAll(),
      api.speakers.getAll(),
      api.locations.getAll()
    ]);

    // Separate upcoming and past meetups
    const now = new Date();
    const upcomingMeetups = allMeetups
      .filter((m) => new Date(m.startUtc) >= now)
      .sort((a, b) => new Date(a.startUtc).getTime() - new Date(b.startUtc).getTime());

    const pastMeetups = allMeetups
      .filter((m) => new Date(m.startUtc) < now)
      .sort((a, b) => new Date(b.startUtc).getTime() - new Date(a.startUtc).getTime())
      .slice(0, 5); // Only show last 10 past events
    
    const activeLocations = locations.filter(loc => loc.isActive);

    return {
      upcomingMeetups,
      pastMeetups,
      speakers: speakers,
      locations: activeLocations
    };
  } catch (error) {
    console.error('Failed to load data:', error);
    // Return empty data if API fails
    return {
      upcomingMeetups: [],
      pastMeetups: [],
      speakers: [],
      locations: []
    };
  }
};
