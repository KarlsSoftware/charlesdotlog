# Angular — Reference

## File Architecture

```
Frontend/src/app/
├── app.config.ts            # bootstrapApplication providers (HttpClient, Router, interceptors)
├── app.routes.ts            # Route definitions (lazy-loaded admin, guarded routes)
├── app.component.ts         # Root component — contains <router-outlet>
├── models/
│   └── post.model.ts        # TypeScript interfaces mirroring C# entities
├── services/
│   ├── post.service.ts      # HTTP calls for /api/posts
│   └── auth.service.ts      # Login/logout, JWT storage, auth signal
├── interceptors/
│   └── auth.interceptor.ts  # Adds Authorization header to every request
├── guards/
│   └── auth.guard.ts        # Redirects to /admin/login if not authenticated
└── components/
    ├── post-list/           # Public: list of all posts
    ├── post-detail/         # Public: single post view
    └── admin/
        ├── login/           # Login form
        ├── dashboard/       # Admin post list + delete
        └── post-form/       # Create / edit post
```

---

## Standalone Components

No NgModule. Every component declares its own dependencies in `imports: []`:

```typescript
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],   // only what this component actually uses
  template: `...`,
})
export class LoginComponent { ... }
```

---

## Services & `inject()`

Services registered with `providedIn: 'root'` are application-wide singletons (equivalent to `AddSingleton` in .NET).

```typescript
@Injectable({ providedIn: 'root' })
export class PostService {
  private http = inject(HttpClient);   // modern functional DI — no constructor needed
  private apiUrl = 'http://localhost:5192/api/posts';

  getPosts(): Observable<BlogPost[]> {
    return this.http.get<BlogPost[]>(this.apiUrl);
  }
}
```

`inject()` works in any injection context (service, guard, interceptor). Constructor DI still works — both are valid.

---

## Observables

An Observable is a lazy stream — unlike a Promise, it doesn't execute until something subscribes to it. `http.get<T>()` returns `Observable<T>`, not the data directly.

The `$` suffix is convention for Observable variables (`posts$`). Two ways to consume:
- `async` pipe in the template — preferred; auto-subscribes and auto-unsubscribes on component destroy
- `.subscribe()` in code — use when you need to trigger side effects imperatively

---

## HttpClient

Register once in `app.config.ts` — available everywhere via DI:

```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withFetch(),                        // uses Fetch API (required for SSR)
      withInterceptors([authInterceptor]) // functional interceptors registered here
    ),
  ],
};
```

Typed requests — generic parameter shapes the response:

```typescript
this.http.get<BlogPost[]>(this.apiUrl)                    // Observable<BlogPost[]>
this.http.get<BlogPost>(`${this.apiUrl}/${id}`)           // Observable<BlogPost>
this.http.post<BlogPost>(this.apiUrl, payload)            // Observable<BlogPost>
this.http.put<BlogPost>(`${this.apiUrl}/${id}`, payload)  // Observable<BlogPost>
this.http.delete<void>(`${this.apiUrl}/${id}`)            // Observable<void>
```

---

## Signals

Angular's reactive primitive — replaces manual change detection triggers.

```typescript
// Create
const count = signal(0);

// Read (calling it like a function)
count()   // → 0

// Write
count.set(1);
count.update(v => v + 1);

// Derived — recalculates when dependencies change
const doubled = computed(() => count() * 2);

// Side effect — runs whenever signal values it reads change
effect(() => console.log('count is', count()));
```

In `AuthService`:

```typescript
isLoggedIn = signal(!!sessionStorage.getItem(this.tokenKey));

login(password: string) {
  return this.http.post<{ token: string }>(...).pipe(
    tap(res => {
      sessionStorage.setItem(this.tokenKey, res.token);
      this.isLoggedIn.set(true);
    })
  );
}

logout() {
  sessionStorage.removeItem(this.tokenKey);
  this.isLoggedIn.set(false);
}
```

`.pipe()` chains RxJS operators onto an Observable. `tap()` runs a side effect — here, storing the token and updating the signal — without changing the emitted value. Without `tap`, you'd need to `.subscribe()` and handle it manually.

Components reading `auth.isLoggedIn()` re-render automatically when it changes.

---

## Async Pipe

Subscribes to an Observable in the template and unsubscribes automatically on component destroy:

```typescript
// Component
export class PostListComponent {
  posts$ = inject(PostService).getPosts();   // Observable<BlogPost[]>
}
```

```html
<!-- Template -->
@if (posts$ | async; as posts) {
  @for (post of posts; track post.id) {
    <div>{{ post.title }}</div>
  }
}
```

Beats `.subscribe()` in components because there's no cleanup needed and no risk of memory leaks.

---

## HTTP Interceptor

Functional interceptor — no class, just a function matching `HttpInterceptorFn`:

```typescript
// interceptors/auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req);
};
```

HTTP requests are immutable — `req.clone({...})` creates a modified copy rather than mutating the original. Registered in `app.config.ts` via `withInterceptors([authInterceptor])`. All outgoing requests automatically get the `Authorization` header when a token exists — no manual header management in services.

---

## Route Guard

Functional guard matching `CanActivateFn`:

```typescript
// guards/auth.guard.ts
export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  return router.createUrlTree(['/admin/login']);
};
```

---

## Lazy Loading

Admin components are not bundled with the main chunk — downloaded only when the route is first accessed:

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: '',           component: PostListComponent },    // eager
  { path: 'posts/:id',  component: PostDetailComponent }, // eager

  {
    path: 'admin/login',
    loadComponent: () =>
      import('./components/admin/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/admin/dashboard/dashboard.component').then(m => m.DashboardComponent),
  },
  {
    path: 'admin/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/admin/post-form/post-form.component').then(m => m.PostFormComponent),
  },
  {
    path: 'admin/edit/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/admin/post-form/post-form.component').then(m => m.PostFormComponent),
  },
];
```

---

## Auth Pattern — How the Pieces Wire Together

```
Login form
  → AuthService.login(password)
    → POST /api/auth/login
    → stores token in sessionStorage
    → isLoggedIn.set(true)

Every HTTP request
  → authInterceptor reads AuthService.getToken()
  → clones request with Authorization: Bearer <token>

Protected routes (/admin, /admin/new, /admin/edit/:id)
  → authGuard reads auth.isLoggedIn()
  → false → redirect to /admin/login
  → true  → allow navigation
```

`sessionStorage` (not `localStorage`) — token is cleared automatically when the tab closes.

