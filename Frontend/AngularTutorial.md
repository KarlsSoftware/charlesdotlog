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

## Template Control Flow (`@if`, `@for`)

Angular 17+ replaced `*ngIf` / `*ngFor` directives with built-in block syntax:

```html
<!-- @if / @else — conditional rendering -->
@if (posts$ | async; as posts) {
  <!-- posts is the unwrapped, non-null value -->
} @else {
  <p>Loading...</p>
}

<!-- @for — loop with required `track` expression -->
@for (post of posts; track post.id) {
  <div>{{ post.title }}</div>
}
```

`track post.id` tells Angular's renderer how to identify each item across re-renders.
Without it, Angular would destroy and recreate every DOM node on every change.
With it, Angular reuses existing DOM nodes and only updates what changed — faster.

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

## Built-In Pipes

Pipes transform values in templates using the `|` character. Angular ships several built-in pipes:

```html
<!-- | date: formats a Date object or ISO string -->
{{ post.createdAt | date:'dd MMM yyyy' }}   <!-- "23 Feb 2026" -->
{{ post.createdAt | date:'dd.MM.yyyy' }}    <!-- "23.02.2026" (German format) -->

<!-- | async: subscribes to an Observable and unwraps the value -->
@if (posts$ | async; as posts) { ... }
```

The `async` pipe also automatically unsubscribes when the component is destroyed — no manual cleanup needed. It's always preferred over `.subscribe()` in templates.

---

## Custom Pipe — `StripHtmlPipe`

A pipe is a simple class decorated with `@Pipe` that implements `PipeTransform`:

```typescript
@Pipe({ name: 'stripHtml', standalone: true })
export class StripHtmlPipe implements PipeTransform {
  transform(value: string, limit = 0): string {
    // ...
  }
}
```

Usage in a template (`:160` is the second argument passed to `transform()`):

```html
{{ post.content | stripHtml:160 }}
```

Pipes must be listed in `imports: [StripHtmlPipe]` of the component that uses them.
Pipes are pure by default — Angular only re-runs them when the input value changes.

---

## Two-Way Binding — `[(ngModel)]`

The "banana in a box" syntax combines property binding and event binding:

```html
<input [(ngModel)]="title" name="title" />
```

This is shorthand for:
```html
<input [ngModel]="title" (ngModelChange)="title = $event" name="title" />
```

- `[ngModel]="title"` → pushes the component's value INTO the input
- `(ngModelChange)="..."` → updates the component whenever the user types

Requires `FormsModule` in the component's `imports` array.
Every `[(ngModel)]` field must have a `name` attribute so Angular Forms can register it.

---

## Template Reference Variables

`#name="directive"` captures a directive instance and makes it available in the template:

```html
<!-- #postForm="ngForm" — the NgForm directive instance for the whole form -->
<form #postForm="ngForm" (ngSubmit)="onSubmit(postForm)">

  <!-- #titleField="ngModel" — the NgModel instance for this specific field -->
  <input [(ngModel)]="title" name="title" required #titleField="ngModel" />

  <!-- .invalid = has validation errors, .touched = user has blurred the field -->
  @if (titleField.invalid && titleField.touched) {
    <p>Title is required.</p>
  }
</form>
```

Using `.touched` prevents showing errors before the user has interacted with the field.

---

## Conditional CSS Classes — `[class.x]`

`[class.someClass]="expression"` adds the class when the expression is truthy, removes it when falsy:

```html
<input
  [class.border-red-400]="field.invalid && field.touched"
  [class.border-neutral-200]="!(field.invalid && field.touched)"
/>
```

This is equivalent to using `[ngClass]` but more readable for single classes.

---

## `[innerHTML]` Binding

Renders a raw HTML string into the DOM:

```html
<div [innerHTML]="post.content"></div>
```

Angular sanitizes the HTML by default (removes `<script>` tags etc.), but takes no further
responsibility for what's displayed. Only use `[innerHTML]` for content you control — never for
user-generated content from untrusted sources.

---

## TypeScript — `Omit<T, K>`

`Omit` is a built-in TypeScript utility type that creates a new type by removing specified keys:

```typescript
// BlogPost has: id, title, content, author, isPublished, createdAt
// Omit removes 'id' and 'createdAt' — the server sets those, not the caller
createPost(post: Omit<BlogPost, 'id' | 'createdAt'>): Observable<BlogPost> {
  return this.http.post<BlogPost>(this.apiUrl, post);
}
```

The function accepts an object with `title`, `content`, `author`, `isPublished` — but TypeScript will give a compile error if you accidentally pass `id` or `createdAt`.

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

---

## SSR (Server-Side Rendering)

The project uses Angular Universal SSR. Two extra config files handle this:

**`app.config.server.ts`** — server-only providers, merged with the main `app.config.ts`:

```typescript
const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(),               // activates Angular Universal
    provideServerRoutesConfig(serverRoutes) // per-route render mode config
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
```

**`app.routes.server.ts`** — which render mode to use per route:

```typescript
export const serverRoutes: ServerRoute[] = [
  { path: '**', renderMode: RenderMode.Client },
];
```

`RenderMode.Client` means all routes use normal client-side rendering — no pre-rendering.
`RenderMode.Prerender` would bake the route into static HTML at build time.

In practice, SSR means the first HTML the browser receives already contains real content
(good for SEO and perceived performance). `provideClientHydration(withEventReplay())` in
`app.config.ts` then "wakes up" Angular on top of that HTML instead of rebuilding the DOM.

