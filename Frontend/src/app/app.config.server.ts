import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering } from '@angular/platform-server';
import { provideServerRoutesConfig } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';

// SSR (Server-Side Rendering): Angular renders each page to HTML on the server
// before sending it to the browser. The user sees real content immediately instead of
// a blank page waiting for JavaScript to download and execute.
// This config is only used during the server-side render pass — never in the browser.
const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(),               // activates Angular Universal SSR
    provideServerRoutesConfig(serverRoutes) // tells Angular which routes to SSR vs CSR
  ]
};

// Merge server-only providers with the shared app providers from app.config.ts.
// The browser bundle uses appConfig alone; the SSR build uses this merged config.
export const config = mergeApplicationConfig(appConfig, serverConfig);
