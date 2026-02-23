# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal blog with an ASP.NET Core Minimal API backend and Angular 19 frontend.

- **Backend:** `Backend/` — .NET 9 Minimal API, SQLite via EF Core, JWT auth
- **Frontend:** `Frontend/` — Angular 19 standalone components, Tailwind CSS v4, Quill rich-text editor
- **Deployment:** Backend → MonsterASP.NET (`charleslog.runasp.net`), Frontend → Vercel (`carlodotlog.vercel.app`)

## Development Commands

### Backend
```bash
cd Backend
dotnet run            # starts API at http://localhost:5192
dotnet build          # build only
dotnet publish -c Release -o ./publish   # production build for deployment
```
> `UseAppHost=false` is set for Debug builds to prevent Avast from locking `apphost.exe`. This does not affect Release/publish.

### Frontend
```bash
cd Frontend
ng serve              # dev server at http://localhost:4200
ng build              # production build → dist/blog-frontend/browser/
```

### Required: `Backend/.env`
```
JWT_SECRET=...
ADMIN_PASSWORD=...
```
The app throws on startup without these.

## Architecture

### Backend (`Backend/Program.cs`)
All API logic lives in a single file. No controllers.

- `GET /api/posts` — published posts only, public
- `GET /api/posts/{id}` — single published post, public (404 for drafts)
- `GET /api/posts/admin` — all posts including drafts, requires JWT
- `GET /api/posts/admin/{id}` — single post by id regardless of publish status, requires JWT
- `POST /api/posts/admin/{id}` — admin edit form uses this endpoint to load drafts
- `POST /api/auth/login` — validates `ADMIN_PASSWORD`, returns JWT (8h expiry)
- Protected endpoints use `.RequireAuthorization()`

**Data layer:** `AppDbContext` (`Backend/Data/`) has one `DbSet<BlogPost>`. DTOs (`CreateBlogPostDto`, `UpdateBlogPostDto`) are defined in `Backend/Models/BlogPost.cs` alongside the entity.

### Frontend (`Frontend/src/app/`)

**Auth flow:** `AuthService` stores JWT in `sessionStorage`. `authInterceptor` attaches `Authorization: Bearer` to every request. `authGuard` protects `/admin/*` routes.

**Public routes** (`PostListComponent`, `PostDetailComponent`) are eagerly loaded. **Admin routes** (`/admin`, `/admin/new`, `/admin/edit/:id`) are lazy-loaded.

**Key pattern:** `PostDetailComponent` strips `&nbsp;` from post content before rendering to fix word-wrapping issues caused by pasted content:
```ts
map(post => ({ ...post, content: post.content.replace(/&nbsp;/g, ' ') }))
```

**Admin edit form** (`PostFormComponent`) calls `postService.getPostAdmin(id)` (not `getPost`) so drafts can be loaded for editing.

**Content rendering:** Quill editor output is stored as raw HTML in the database. Rendered via `[innerHTML]` inside `.ql-content` CSS class. Quill Snow CSS is loaded globally (`angular.json`).

**Environment switching:** `src/environments/environment.ts` (dev, `localhost:5192`) vs `environment.prod.ts` (prod, `charleslog.runasp.net`). Angular CLI swaps them on `ng build`.

## Deployment

See `deployment.md` for full step-by-step instructions. Summary:
- Backend: `dotnet publish -c Release -o ./publish`, FTP upload to MonsterASP `wwwroot`, include `.env` and `blog.db`
- Frontend: auto-deploys on push to `master` via Vercel

**When changing the Vercel URL**, update both `Backend/Program.cs` CORS origins and `deployment.md`.

# Use Context7 for current library docs 
