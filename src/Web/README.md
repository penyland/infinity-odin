# .NET Skåne Website

A SvelteKit website for the .NET Skåne meetup group, styled with TailwindCSS.

## Features

- **Upcoming Events**: Displays upcoming meetups with date, title, and location
- **Past Events**: Shows a list of past meetups
- **Speakers**: Showcases meetup speakers with avatars
- **Sponsors**: Displays sponsor information
- **API Integration**: Fetches real-time data from the Odin API

## Tech Stack

- **SvelteKit** (latest version)
- **TailwindCSS** with Typography plugin
- **TypeScript**
- **Vite**

## Setup

1. Install dependencies:
   ```bash
   npm install
   ```

2. Configure the API endpoint:
   - Update `.env` file with your API URL
   - Default: `VITE_API_URL=https://localhost:7119`

3. Run the development server:
   ```bash
   npm run dev
   ```

4. Open your browser at `http://localhost:5173`

## Project Structure

```
src/
├── lib/
│   └── api/
│       ├── client.ts    # API client for fetching data
│       └── types.ts     # TypeScript types from OpenAPI spec
├── routes/
│   ├── +layout.svelte   # Root layout
│   ├── +page.svelte     # Homepage component
│   └── +page.ts         # Data loading logic
├── app.css              # Global styles and TailwindCSS config
└── app.html             # HTML template
```

## API Integration

The website connects to the Odin API to fetch:
- Meetups (upcoming and past)
- Speakers
- Locations
- Presentations

API client is located at `src/lib/api/client.ts` and provides typed methods for all endpoints.

## Styling

The design matches the original screenshot with:
- Clean, minimal layout
- Handwritten-style headings using Caveat font
- Black borders for event cards
- Responsive grid layout
- Typography optimized for readability

## Developing

Once you've created a project and installed dependencies with `npm install` (or `pnpm install` or `yarn`), start a development server:

```sh
npm run dev

# or start the server and open the app in a new browser tab
npm run dev -- --open
```

## Building

To create a production version of your app:

```sh
npm run build
```

You can preview the production build with `npm run preview`.

> To deploy your app, you may need to install an [adapter](https://svelte.dev/docs/kit/adapters) for your target environment.

