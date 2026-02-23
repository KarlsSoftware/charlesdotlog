import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './interceptors/auth.interceptor';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    /**
     * provideHttpClient registers Angular's HttpClient for DI —
     * like builder.Services.AddHttpClient() in .NET.
     * withFetch() uses the modern Fetch API (needed for SSR).
     */
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
  ],
};
