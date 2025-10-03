// Generated types from OpenAPI specification

export interface Info {
	name?: string | null;
	version?: string | null;
	dateTime?: string;
	environment?: string | null;
	frameworkDescription?: string | null;
	osVersion?: string | null;
	buildDate?: string | null;
	osArchitecture?: string | null;
	runtimeIdentifier?: string | null;
}

export interface FeatureModuleInfo {
	name: string;
	version: string;
}

export interface LocationDto {
	locationId: string;
	name: string;
	street?: string | null;
	city?: string | null;
	postalCode?: string | null;
	country?: string | null;
	description?: string | null;
	maxCapacity?: number | null;
	isActive: boolean;
}

export interface RsvpDto {
	totalSpots: number;
	rsvpYesCount: number;
	rsvpNoCount: number;
	rsvpWaitlistCount: number;
	attendanceCount: number;
}

export interface SpeakerBioDto {
	speakerBioId: string;
	speakerId: string;
	bio: string;
	isPrimary: boolean;
}

export interface SpeakerDto {
	speakerId: string;
	fullName: string;
	company?: string | null;
	twitterUrl?: string | null;
	gitHubUrl?: string | null;
	linkedInUrl?: string | null;
	blogUrl?: string | null;
	thumbnailUrl?: string | null;
	bio?: string | null;
}

export interface PresentationDto {
	presentationId: string;
	title: string;
	abstract?: string | null;
	speakers?: SpeakerDto[] | null;
}

export interface MeetupDto {
	meetupId: string;
	title: string;
	description: string;
	startUtc: string;
	endUtc: string;
	rsvp: RsvpDto;
	location: LocationDto;
	presentations?: PresentationDto[] | null;
}
