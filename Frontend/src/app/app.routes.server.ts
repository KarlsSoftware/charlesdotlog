import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    // '**' matches every route — all pages use client-side rendering (CSR).
    // RenderMode.Client means Angular does not pre-render HTML on the server;
    // the browser receives the same index.html shell and boots Angular itself.
    // Change to RenderMode.Prerender for static routes that should be fully
    // baked into HTML at build time (e.g. a static "About" page).
    path: '**',
    renderMode: RenderMode.Client,
  },
];
