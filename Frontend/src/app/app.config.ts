import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './interceptors/auth.interceptor';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    // Zone.js is Angular's change detection engine — it patches browser APIs (setTimeout,
    // Promise, click events, etc.) to know when async work completes and re-render the UI.
    // eventCoalescing: true batches multiple DOM events fired in the same microtask into one
    // change detection cycle instead of running it separately for each event (performance boost).
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // SSR hydration: after the server sends pre-rendered HTML, Angular "hydrates" it by
    // attaching event listeners to the existing DOM instead of rebuilding the whole page.
    // withEventReplay() records user interactions (clicks, keystrokes) that happen before
    // hydration finishes, then replays them — so fast-clicking users don't lose their actions.
    provideClientHydration(withEventReplay()),
    /**
     * provideHttpClient registers Angular's HttpClient for DI —
     * like builder.Services.AddHttpClient() in .NET.
     * withFetch() uses the modern Fetch API (needed for SSR).
     */
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
  ],
};
