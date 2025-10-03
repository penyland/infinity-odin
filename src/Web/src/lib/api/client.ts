import type {
  MeetupDto,
  SpeakerDto,
  LocationDto,
  PresentationDto,
  RsvpDto,
  SpeakerBioDto
} from './types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5016';

async function fetchApi<T>(endpoint: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`);
  if (!response.ok) {
    throw new Error(`API request failed: ${response.statusText}`);
  }
  return response.json();
}

export const api = {
  meetups: {
    getAll: (status?: string) => {
      const query = status ? `?Status=${status}` : '';
      return fetchApi<MeetupDto[]>(`/meetupplanner/meetups${query}`);
    },
    getById: (meetupId: string) =>
      fetchApi<MeetupDto>(`/meetupplanner/meetups/${meetupId}`),
    getLocation: (meetupId: string) =>
      fetchApi<LocationDto>(`/meetupplanner/meetups/${meetupId}/location`),
    getPresentations: (meetupId: string) =>
      fetchApi<PresentationDto>(`/meetupplanner/meetups/${meetupId}/presentations`),
    getRsvps: (meetupId: string) =>
      fetchApi<RsvpDto>(`/meetupplanner/meetups/${meetupId}/rsvps`)
  },
  speakers: {
    getAll: () => fetchApi<SpeakerDto[]>('/meetupplanner/speakers'),
    getById: (speakerId: string) =>
      fetchApi<SpeakerDto>(`/meetupplanner/speakers/${speakerId}`),
    getBios: (speakerId: string) =>
      fetchApi<SpeakerBioDto[]>(`/meetupplanner/speakers/${speakerId}/bios`),
    getPresentations: (speakerId: string) =>
      fetchApi<PresentationDto[]>(`/meetupplanner/speakers/${speakerId}/presentations`)
  },
  locations: {
    getAll: () => fetchApi<LocationDto[]>('/meetupplanner/locations'),
    getById: (locationId: string) =>
      fetchApi<LocationDto>(`/meetupplanner/locations/${locationId}`)
  },
  presentations: {
    getAll: () => fetchApi<PresentationDto[]>('/meetupplanner/presentations')
  }
};
