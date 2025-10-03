<script lang="ts">
  import type { LocationDto, MeetupDto, SpeakerDto } from '$lib/api/types';

  interface PageData {
    upcomingMeetups: MeetupDto[];
    pastMeetups: MeetupDto[];
    speakers: SpeakerDto[];
    locations: LocationDto[];
  }

  let { data }: { data: PageData } = $props();

  // Helper function to format date
  function formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('sv-SE', { year: 'numeric', month: 'numeric', day: 'numeric' });
  }
</script>

<svelte:head>
  <title>Welcome to .NET Skåne</title>
</svelte:head>

<div class="min-h-screen bg-white">
  <!-- Main Container -->
  <div class="mx-auto max-w-4xl px-8 py-12">
    <!-- Header -->
    <header class="mb-12">
      <h1 class="mb-4 text-5xl font-bold">Welcome to .NET Skåne</h1>
      <div class="space-y-1 text-gray-700">
        <p>
          This is a group for anybody interested in learning, and sharing experiences around using
          .NET technologies.
        </p>
        <p>Join us for the opportunity to learn from each other.</p>
        <p>
          The ambition is to keep a low bar for anybody to be able to share what they're working on.
        </p>
      </div>
    </header>

    <!-- Upcoming Events Section -->
    <section class="mb-12">
      <h2 class="mb-6 text-3xl font-bold">Upcoming events</h2>
      <div class="grid grid-cols-2 gap-6 lg:grid-cols-1">
        {#each data.upcomingMeetups as meetup}
          <div class="rounded-sm p-6 shadow-lg hover:shadow-xl transition">
            <div class="mb-2">
              <div class="mb-3">
                <span class="mb-2 text-sm italic">
                  {meetup.startUtc
                    ? `${new Date(meetup.startUtc).toLocaleDateString('en-US', { weekday: 'long', month: 'numeric', day: 'numeric', year: 'numeric' })}`
                    : ''}
                </span>
                <span class="text-sm text-gray-600">@{meetup.location.name}</span>
              </div>
              <div class="text-lg font-bold">{meetup.title}</div>
              <div class="text-md mt-2 text-gray-600">Agenda:</div>
              <ul>
                {#each meetup.presentations ?? [] as presentation}
                  <li class="mt-2">
                    <div class="text-md mt-1">
                      {presentation.title} - {(presentation.speakers ?? [])
                        .map((s) => s.fullName)
                        .join(', ')}
                    </div>
                  </li>
                {/each}
              </ul>
            </div>
          </div>
        {:else}
          <div>No upcoming meetups are planned yet!</div>
        {/each}
      </div>
    </section>

    <!-- Past Events Section -->
    <section class="mb-12">
      <div class="mb-4 flex items-baseline justify-between gap-4">
        <div>
          <span class="text-2xl font-bold">Past Events</span>
          <span class="text-md mb-2 ml-2 text-gray-600">{data.pastMeetups.length}</span>
        </div>
        <span class="text-sm text-gray-500">See all</span>
      </div>
      <div class="space-y-2 text-gray-700">
        {#each data.pastMeetups as meetup}
          <div class="">
            {formatDate(meetup.startUtc)} -
            {meetup.title}
          </div>
        {:else}
          <div>No past events found</div>
        {/each}
      </div>
    </section>

    <!-- Meet our Speakers Section -->
    <section class="mb-12">
      <h2 class="mb-6 text-2xl font-bold">Meet our Speakers</h2>
      <div class="grid grid-cols-2 gap-8 md:grid-cols-6">
        {#each data.speakers as speaker}
          <div class="flex flex-col items-center">
            <div
              class="mb-2 flex h-16 w-16 items-center justify-center rounded-full border-2 border-gray-200 shadow-lg hover:shadow-xl hover:border-gray-400 transition"
            >
              {#if speaker.thumbnailUrl}
              {console.log(speaker.thumbnailUrl)}
                <img
                  src="{speaker.thumbnailUrl}"
                  alt="{speaker.fullName}"
                  class="h-16 w-16 rounded-full object-cover"
                />
              {:else}
                <span class="text-2xl">👤</span>
              {/if}
            </div>
            <div class="text-center text-sm">{speaker.fullName}</div>
          </div>
        {:else}
          No Speakers found
        {/each}
      </div>
    </section>

    <!-- Our Sponsors Section -->
    <section class="mb-12">
      <h2 class="mb-6 text-2xl font-normal">Our Sponsors</h2>
      <div class="flex gap-8 text-gray-600">
        {#each data.locations as location}
          <div>{location.name}</div>
        {:else}
          <div>No Sponsors found</div>
        {/each}
      </div>
    </section>

    <!-- Footer -->
    <footer class="mt-16 text-center text-sm text-gray-500">Copyright 2025 .NET Skåne</footer>
  </div>
</div>
