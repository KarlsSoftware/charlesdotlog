import { RenderMode, PrerenderFallback, ServerRoute } from '@angular/ssr';
import { inject } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { PostService } from './services/post.service';

export const serverRoutes: ServerRoute[] = [
  {
    path: '',
    renderMode: RenderMode.Prerender,
  },
  {
    path: 'posts/:id',
    renderMode: RenderMode.Prerender,
    fallback: PrerenderFallback.Client,
    async getPrerenderParams() {
      const postService = inject(PostService);
      const posts = await lastValueFrom(postService.getPosts());
      return (posts ?? []).map((post) => ({ id: String(post.id) }));
    },
  },
  {
    path: 'admin/**',
    renderMode: RenderMode.Client,
  },
  {
    path: '**',
    renderMode: RenderMode.Client,
  },
];
