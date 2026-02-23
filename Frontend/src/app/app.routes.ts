import { Routes } from '@angular/router';
import { PostListComponent } from './components/post-list/post-list.component';
import { PostDetailComponent } from './components/post-detail/post-detail.component';
import { authGuard } from './guards/auth.guard';

/**
 * Angular routing = frontend equivalent of app.MapGet() in .NET.
 * Maps URL paths to components instead of handler functions.
 *
 * Admin routes use:
 * - authGuard: redirects to /admin/login if not authenticated
 * - loadComponent: lazy loading — code is only downloaded when needed
 */
export const routes: Routes = [
  { path: '', component: PostListComponent },
  { path: 'posts/:id', component: PostDetailComponent },

  // Admin routes
  {
    path: 'admin/login',
    loadComponent: () =>
      import('./components/admin/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/admin/dashboard/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'admin/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/admin/post-form/post-form.component').then((m) => m.PostFormComponent),
  },
  {
    path: 'admin/edit/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/admin/post-form/post-form.component').then((m) => m.PostFormComponent),
  },
];
