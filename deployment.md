# Deployment Guide

## Stack
- **Backend:** ASP.NET Core Minimal API (.NET 9) → MonsterASP.NET (`charleslog.runasp.net`)
- **Frontend:** Angular 19 SPA → Vercel (`charlesdotlog.vercel.app`)
- **Database:** SQLite (`blog.db`) — persists on MonsterASP.NET file system

---

## First-Time Setup (already done — for reference)

### Code changes that were made once
- Added `Frontend/src/environments/environment.ts` (dev API URL)
- Added `Frontend/src/environments/environment.prod.ts` (production API URL)
- Updated `angular.json` with `fileReplacements` so prod build uses prod environment
- Updated services to use `environment.apiUrl` instead of hardcoded localhost
- Added `Frontend/vercel.json` for Angular client-side routing
- CORS in `Backend/Program.cs` hardcoded to allow localhost + Vercel URL

### Vercel project settings (set once in dashboard)
- **Root Directory:** `Frontend`
- **Output Directory:** `dist/blog-frontend/browser`
- Framework: Angular (auto-detected)

---

## Backend Deployment (MonsterASP.NET)

### 1. Publish the app locally
```bash
cd C:\netminimalapi\Backend
dotnet publish -c Release -o ./publish
```
Output goes to `C:\netminimalapi\Backend\publish\`

### 2. Upload via FTP to MonsterASP.NET
- **FTP credentials:** in MonsterASP.NET control panel
- **Target folder:** `wwwroot`
- Upload **all files** from `Backend/publish/` → overwrite everything in `wwwroot`

### 3. Upload secrets file
Upload `Backend/.env` to `wwwroot` (never commit this file — it contains secrets).
The `.env` file must contain:
```
JWT_SECRET=...
ADMIN_PASSWORD=...
```
> See `.env` file locally — do NOT put actual values in this document.

### 4. Upload database (first deploy only, or when migrating data)
Upload these files to `wwwroot`:
- `Backend/blog.db`
- `Backend/blog.db-shm` _(if it exists)_
- `Backend/blog.db-wal` _(if it exists)_

> On subsequent deploys you do NOT re-upload the database — it lives on the server.

### 5. Enable HTTPS (first time only)
In MonsterASP.NET control panel → SSL → Enable Let's Encrypt.
Wait ~3 minutes for the certificate to activate (site will be briefly down).

### 6. Verify backend
Visit `https://charleslog.runasp.net/api/posts` — should return JSON.

---

## Frontend Deployment (Vercel)

Vercel auto-deploys on every push to GitHub (`master` branch).

To trigger a manual redeploy: Vercel dashboard → project → **Redeploy**.

### Verify frontend
Visit `https://charlesdotlog.vercel.app` — blog should load and show posts.

---

## Updating CORS (if Vercel URL ever changes)

Edit `Backend/Program.cs` — find the CORS block:
```csharp
policy.WithOrigins(
    "http://localhost:4200",
    "https://charlesdotlog.vercel.app"   // ← update this
)
```
Then re-publish and re-upload (steps 1–2 above).

---

## Common Issues

| Symptom | Cause | Fix |
|---|---|---|
| `Failed to fetch` / status 0 on frontend | CORS not matching | Check CORS origins in `Program.cs`, re-publish |
| Site down after enabling SSL | Let's Encrypt provisioning | Wait 2–3 minutes |
| Posts not showing after deploy | Database not uploaded | Upload `blog.db` via FTP |
| Old code still running after FTP upload | IIS cached old app | Re-upload `web.config` to force restart |
| Build fails with "file in use" | Dev server is running | Stop `dotnet run` first, then publish |
